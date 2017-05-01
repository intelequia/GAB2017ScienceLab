<#
    .DESCRIPTION
        Setups the Automation job schedules using the Run As Account (Service Principal)
        for the Azure Bootcamp 2017 Science Lab - https://global.azurebootcamp.net/science-lab-2017/

    .NOTES
        AUTHORS: 
            David Rodriguez (@davidjrh - http://davidjrh.intelequia.com)
            Adonai Suarez (@adonaisg)
        LASTEDIT: Apr 7, 2017
#>
param (
    [Parameter(Mandatory=$true)]
    #[Description("The resource group name")]
    [string]$resourceGroupName,

    [Parameter(Mandatory=$true)]
    #[Description("The automation account name")]
    [string]$batchAccountName,


    [Parameter(Mandatory=$true)]
    #[Description("The automation account name")]
    [string]$automationAccountName,

    [Parameter(Mandatory=$true)]
    #[Description("The name of the runbook to obtain a new batch of inputs")]
    [string]$runbookName = "Get-NewBatch"
)

$ErrorActionPreference = "Stop"
$connectionName = "AzureRunAsConnection"
try
{
    # Get the connection "AzureRunAsConnection "
    $servicePrincipalConnection=Get-AutomationConnection -Name $connectionName         

    "Logging in to Azure..."
    Login-AzureRmAccount `
        -ServicePrincipal `
        -TenantId $servicePrincipalConnection.TenantId `
        -ApplicationId $servicePrincipalConnection.ApplicationId `
        -CertificateThumbprint $servicePrincipalConnection.CertificateThumbprint 
}
catch {
    if (!$servicePrincipalConnection)
    {
        $ErrorMessage = "Connection $connectionName not found."
        throw $ErrorMessage
    } else{
        Write-Error -Message $_.Exception
        throw $_.Exception
    }
}

"Registering Microsoft.Automation resource provider..."
#Register-AzureRMResourceProvider -ProviderNamespace Microsoft.Automation


"Verifying schedules..."
$currentDate = Get-Date

# start time must be at least 5 minutes after schedule creation
$currentDate = $currentDate.AddMinutes(10)

# stop the schedules after Bootcamp weekend
$expiryTime = Get-Date -Date '2017-04-24'

# We have to create 12 schedules if we want 5 minute resolution (minutes not supported on Automation)
for ($i = 0; $i -lt 60; $i+=5) {
    $schedulename = "Schedule" + $i.ToString("00")

    # Check schedule
    "Creating schedule $schedulename..."
    Remove-AzureRmAutomationSchedule -ResourceGroupName $resourceGroupName -AutomationAccountName $automationAccountName -Name $schedulename -Force -ErrorAction SilentlyContinue
    New-AzureRmAutomationSchedule -ResourceGroupName $resourceGroupName -AutomationAccountName $automationAccountName -Name $schedulename `
            -StartTime $currentDate.AddMinutes($i) -ExpiryTime $expiryTime -HourInterval 1
    $schedule = Get-AzureRmAutomationSchedule -ResourceGroupName $resourceGroupName -AutomationAccountName $automationAccountName -Name $schedulename 

    
    # Check scheduled runbook
    $scheduledRunbook = $null
    $scheduledRunbook = Get-AzureRmAutomationScheduledRunbook -ResourceGroupName $resourceGroupName -AutomationAccountName $automationAccountName `
                    -ScheduleName $schedulename -RunbookName $runbookName -ErrorAction SilentlyContinue
    if ($scheduledRunbook -eq $null) {
        "Creating schedule $schedulename for runbook $runbookName..."
        $parameters = @{"batchAccountName"=$batchAccountName; "automationAccountName"=$automationAccountName}
        Register-AzureRmAutomationScheduledRunbook -ResourceGroupName $resourceGroupName -AutomationAccountName $automationAccountName `
            -ScheduleName $schedulename -RunbookName $runbookName -Parameters $parameters
    }
}

"Job schedules created successfully!"