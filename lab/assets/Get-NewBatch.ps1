<#
    .DESCRIPTION
        Setups the Automation job schedules using the Run As Account (Service Principal)
        for the Azure Bootcamp 2017 Science Lab - https://global.azurebootcamp.net/science-lab-2017/

    .NOTES
        AUTHORS: 
            David Rodriguez (@davidjrh - http://davidjrh.intelequia.com)
            Adonai Suarez (@adonaisg)
        LASTEDIT: Apr 2, 2017
#>
param (
    [Parameter(Mandatory=$true)]
    [string]$batchAccountName,

    [Parameter(Mandatory=$true)]
    [string]$automationAccountName
)

$ErrorActionPreference = "Stop"
$AzureBatchAccount = $batchAccountName
$AutomationAccount = $automationAccountName
$AzureBatchPoolId = "$batchAccountName-pool"            

$connectionName = "AzureRunAsConnection"

try {
    # Get the connection "AzureRunAsConnection "
    $servicePrincipalConnection=Get-AutomationConnection -Name $connectionName         
    Login-AzureRmAccount `
        -ServicePrincipal `
        -TenantId $servicePrincipalConnection.TenantId `
        -ApplicationId $servicePrincipalConnection.ApplicationId `
        -CertificateThumbprint $servicePrincipalConnection.CertificateThumbprint 
} catch {
    if (!$servicePrincipalConnection) {
        $ErrorMessage = "Connection $connectionName not found."
        throw $ErrorMessage
    } else {
        Write-Error -Message $_.Exception
        throw $_.Exception
    }
}
   
$Context = Get-AzureRmBatchAccountKeys -AccountName $AzureBatchAccount
# get count of nodes
$Measure = Get-AzureBatchPool -Id $AzureBatchPoolId -BatchContext $Context | Get-AzureBatchComputeNode -BatchContext $Context | measure
$NodesCount = $Measure.Count

# Set the factor for the task pool
$AzureBatchPool = Get-AzureBatchPool -Id $AzureBatchPoolId -BatchContext $Context
switch -Wildcard ($AzureBatchPool.VirtualMachineSize) {
    "basic_a*" { $TaskPoolFactor = 1 }
    "standard_a*" { $TaskPoolFactor = 1 }
    "standard_d*" { $TaskPoolFactor = 1.5 }
    "standard_d*v2" { $TaskPoolFactor = 2 }
    "standard_f*" { $TaskPoolFactor = 2 }
    "standard_g*" { $TaskPoolFactor = 3 }
    default { $TaskPoolFactor = 1.5 }
}

# get count of pending tasks 
$PendingTasksCount = 0
$PendingJobs = Get-AzureBatchJob -BatchContext $Context -Filter "state eq 'active'" | foreach-object {
    $Measure = Get-AzureBatchTask -JobId $_.Id -BatchContext $Context -Filter "state eq 'active'" | measure 
    $PendingTasksCount += $Measure.Count    
}

$TaskPool = [math]::ceiling($NodesCount * $TaskPoolFactor)     # define "task pool" as 150% of total nodes
$InputsToRequest = $TaskPool - $PendingTasksCount  # calculate tasks needed to fill the "task pool"

if ($InputsToRequest -gt 0) {
    "Requesting $InputsToRequest new inputs..."

    # Get user data for event hub
    $EmailVar = Get-AzureRmAutomationVariable -Name "Email" -ResourceGroupName $Context.ResourceGroupName -AutomationAccountName $AutomationAccount
    $FullNameVar = Get-AzureRmAutomationVariable -Name "FullName" -ResourceGroupName $Context.ResourceGroupName -AutomationAccountName $AutomationAccount
    $TeamNameVar = Get-AzureRmAutomationVariable -Name "TeamName" -ResourceGroupName $Context.ResourceGroupName -AutomationAccountName $AutomationAccount
    $CompanyNameVar = Get-AzureRmAutomationVariable -Name "CompanyName" -ResourceGroupName $Context.ResourceGroupName -AutomationAccountName $AutomationAccount
    $CountryCodeVar = Get-AzureRmAutomationVariable -Name "CountryCode" -ResourceGroupName $Context.ResourceGroupName -AutomationAccountName $AutomationAccount
    $LabKeyCodeVar = Get-AzureRmAutomationVariable -Name "LabKeyCode" -ResourceGroupName $Context.ResourceGroupName -AutomationAccountName $AutomationAccount

    # Get tasks inputs for idle nodes from API
    $Uri = "http://gab2017.trafficmanager.net/api/Batch/GetNewBatch?" + 
    "batchSize=$InputsToRequest" + 
    "&email=" + [uri]::EscapeDataString($EmailVar.Value) + 
    "&fullName=" + [uri]::EscapeDataString($FullNameVar.Value) + 
    "&teamName=" + [uri]::EscapeDataString($TeamNameVar.Value) + 
    "&companyName=" + [uri]::EscapeDataString($CompanyNameVar.Value) + 
    "&location=" + [uri]::EscapeDataString($LabKeyCodeVar.Value) + 
    "&countryCode=" + [uri]::EscapeDataString($CountryCodeVar.Value)

    try {
        $response = Invoke-RestMethod -Method Get -Uri $Uri
    } catch {
        "An error occurred during the request..."        
        if ($_.Exception.Response.StatusCode.value__ -eq 400) {
            $s = $_.Exception.Response.GetResponseStream()
            $s.Position = 0;
            $sr = New-Object System.IO.StreamReader($s)
            $err = $sr.ReadToEnd()
            $sr.Close()
            $s.Close()
        }
        "StatusCode: $($_.Exception.Response.StatusCode.value__)"
        "StatusDescription: $($_.Exception.Response.StatusDescription) ($err)"
        throw $_.Exception
    }
    "Creating tasks for batch $($response.batchId)"

    # Create a new job for each batch and attach tasks to it
    $PoolInformation = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSPoolInformation" 
    $PoolInformation.PoolId = $AzureBatchPoolId
    $JobId = $response.batchId
    New-AzureBatchJob -Id $JobId -DisplayName "Process lab inputs" -PoolInformation $PoolInformation -BatchContext $Context -OnAllTasksComplete NoAction 

    $i = 1
    $response.inputs | ForEach-Object { 
        "Creating task with input $($_.inputId) ($i of $InputsToRequest)"
        $TaskName = "Task-" + $_.inputId 
        $CommandLine = "python3 /tmp/task.py"
        $ResourceFiles = @{"seliga.in" = $_.parameters}
        $EnvironmentSettings = @{"INPUT_ID"=$_.inputId; "USER_EMAIL"=$EmailVar.Value};
        New-AzureBatchTask -BatchContext $Context -JobId $JobID -Id $TaskName -DisplayName $TaskName -CommandLine $CommandLine `
                -ResourceFiles $ResourceFiles -EnvironmentSettings $EnvironmentSettings  
        $i++
    }
    
    $Job = Get-AzureBatchJob -Id $JobId -BatchContext $context
    $Job.OnAllTasksComplete = "Terminate"
    Set-AzureBatchJob -Job $Job -BatchContext $context
             
} else {
    "Nothing to do."
}
