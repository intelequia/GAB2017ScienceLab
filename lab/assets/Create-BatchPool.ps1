<#
    .DESCRIPTION
        Creates the Batch pool for a Azure Batch Account using the Run As Account (Service Principal)
        for the Azure Bootcamp 2017 Science Lab - https://global.azurebootcamp.net/science-lab-2017/

        This runbook uses a modified version of AzureRM.Batch (2.7.1) automation module to allow set the 
        UserIdentity on the StartTask. Check https://github.com/davidjrh/azure-powershell

    .NOTES
        AUTHORS: 
            David Rodriguez (@davidjrh - http://davidjrh.intelequia.com)
            Adonai Suarez (@adonaisg)
        LASTEDIT: Apr 2, 2017
#>
param (
    [Parameter(Mandatory=$true)]
    #[Description("The batch account name")]
    [string]$batchAccountName,

    [Parameter(Mandatory=$false)]
    [ValidateSet("Standard_A1",
        "Standard_A1_v2",
        "Standard_D1",
        "Standard_DS1",
        "Standard_D1_v2",
        "Standard_DS1_v2",
        "Standard_F1",
        "Standard_F1s")]
    [string]$vmSize = "Standard_A1",

    [Parameter(Mandatory=$false)]
    [int]$vmCount = 1
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


# Fixed variables for the setting up the lab pool and related resources
$imageOffer = "UbuntuServer"
$imagePublisher = "Canonical"
$imageSku = "16.04-LTS"
$imageVersion = "latest"
$nodeAgentSkuId = "batch.node.ubuntu 16.04"

$blobSource1 = "http://globalazurebootcamp.blob.core.windows.net/release/setup/starttask.sh"
$filePath1 = "/tmp/starttask.sh"
$blobSource2 = "http://globalazurebootcamp.blob.core.windows.net/release/setup/gablab.zip"
$filePath2 = "/tmp/gablab.zip"
$blobSource3 = "http://globalazurebootcamp.blob.core.windows.net/release/setup/task.py"
$filePath3 = "/tmp/task.py"


"Registering Microsoft.Batch resource provider..."
#Register-AzureRMResourceProvider -ProviderNamespace Microsoft.Batch

"Checking Batch account..."
$context = Get-AzureRmBatchAccountKeys -AccountName $batchAccountName
$poolId = "$batchAccountName-pool"
"Verifying pool existence..."
$pool = Get-AzureBatchPool -Id $poolId -BatchContext $context -ErrorAction SilentlyContinue
if ($pool -eq $null) {
    "Creating pool..."
    $imageReference = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSImageReference" -ArgumentList @($imageOffer,$imagePublisher, $imageSku, $imageVersion)
    $configuration = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSVirtualMachineConfiguration" -ArgumentList @($imageReference,$nodeAgentSkuId)
    New-AzureBatchPool -Id $poolId -VirtualMachineSize $vmSize -VirtualMachineConfiguration $configuration  -TargetDedicated $vmCount -BatchContext $context -MaxTasksPerComputeNode 1
    $pool = Get-AzureBatchPool -Id $poolId -BatchContext $context -ErrorAction SilentlyContinue
}

"Setup start task..."
$userSpec = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSAutoUserSpecification" -ArgumentList @("Task", "Admin")
$userIdentity = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSUserIdentity" -ArgumentList @($userSpec)
$startTask = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSStartTask"
$startTask.CommandLine = "sh /tmp/starttask.sh"
$startTask.WaitForSuccess = $true
$startTask.UserIdentity = $userIdentity
$resourceFiles = New-Object -TypeName "System.Collections.Generic.List[Microsoft.Azure.Commands.Batch.Models.PSResourceFile]"
$resourceFile1 = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSResourceFile" -ArgumentList @($blobSource1, $filePath1, "0777")
$resourceFiles.Add($resourceFile1)
$resourceFile2 = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSResourceFile" -ArgumentList @($blobSource2, $filePath2, "0777")
$resourceFiles.Add($resourceFile2)
$resourceFile3 = New-Object -TypeName "Microsoft.Azure.Commands.Batch.Models.PSResourceFile" -ArgumentList @($blobSource3, $filePath3, "0777")
$resourceFiles.Add($resourceFile3)
$startTask.ResourceFiles = $resourceFiles


$pool.StartTask = $startTask
Set-AzureBatchPool -Pool $pool -BatchContext $context
"Pool setup success!"