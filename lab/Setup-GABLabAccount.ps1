#Requires -RunAsAdministrator
<#
    .DESCRIPTION
        Creates a Run As Account (Service Principal) to be used by the Azure Automation account
        for the Azure Bootcamp 2017 Science Lab - https://global.azurebootcamp.net/science-lab-2017/

    .NOTES
        AUTHORS: 
            David Rodriguez (@davidjrh - http://davidjrh.intelequia.com)
            Adonai Suarez (@adonaisg)
        REFERENCE: https://docs.microsoft.com/en-us/azure/automation/automation-sec-configure-azure-runas-account
        LASTEDIT: Apr 6, 2017
#>
param (
    [Parameter(Mandatory=$true)]
    [String] $SubscriptionId,

    [Parameter(Mandatory=$true)]
    [String] $ResourceGroupName,

    [Parameter(Mandatory=$true)]
	[ValidateSet("East US 2", "South Central US", "West Central US", "North Europe", "West Europe", "Southeast Asia", "Japan East", "Australia Southeast", "Central India", "Canada Central", "UK South")]
    [String] $Location,

    [Parameter(Mandatory=$true)]
    [String] $ApplicationDisplayName,

    [Parameter(Mandatory=$true)]
    [String] $SelfSignedCertPlainPassword,

    [Parameter(Mandatory=$false)]
    [ValidateSet("AzureCloud","AzureUSGovernment")]
    [String]$EnvironmentName="AzureCloud",

    [Parameter(Mandatory=$false)]
    [int] $SelfSignedCertNoOfMonthsUntilExpired = 12
)

function CreateSelfSignedCertificate([string] $certificateName, [string] $selfSignedCertPlainPassword,
                            [string] $certPath, [string] $certPathCer, [string] $selfSignedCertNoOfMonthsUntilExpired ) {

    # WARNING: This seems to work only on Windows 10, makecert.exe would be needed for previous versions
    $Cert = New-SelfSignedCertificate -DnsName $certificateName -CertStoreLocation cert:\LocalMachine\My `
    -KeyExportPolicy Exportable -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
    -NotAfter (Get-Date).AddMonths($selfSignedCertNoOfMonthsUntilExpired)

    $CertPassword = ConvertTo-SecureString $selfSignedCertPlainPassword -AsPlainText -Force
    Export-PfxCertificate -Cert ("Cert:\localmachine\my\" + $Cert.Thumbprint) -FilePath $certPath -Password $CertPassword -Force | Write-Verbose
    Export-Certificate -Cert ("Cert:\localmachine\my\" + $Cert.Thumbprint) -FilePath $certPathCer -Type CERT | Write-Verbose
}

function CreateServicePrincipal([System.Security.Cryptography.X509Certificates.X509Certificate2] $PfxCert, [string] $applicationDisplayName) {  
    $CurrentDate = Get-Date
    $keyValue = [System.Convert]::ToBase64String($PfxCert.GetRawCertData())
    $KeyId = (New-Guid).Guid

    $KeyCredential = New-Object  Microsoft.Azure.Commands.Resources.Models.ActiveDirectory.PSADKeyCredential
    $KeyCredential.StartDate = $CurrentDate
    $KeyCredential.EndDate= $PfxCert.NotAfter # [DateTime]$PfxCert.GetExpirationDateString()
    $KeyCredential.KeyId = $KeyId
    $KeyCredential.CertValue  = $keyValue

    # Use key credentials and create an Azure AD application
    $Application = New-AzureRmADApplication -DisplayName $ApplicationDisplayName -HomePage ("http://" + $applicationDisplayName) -IdentifierUris ("http://" + $KeyId) -KeyCredentials $KeyCredential
    $ServicePrincipal = New-AzureRMADServicePrincipal -ApplicationId $Application.ApplicationId
    $GetServicePrincipal = Get-AzureRmADServicePrincipal -ObjectId $ServicePrincipal.Id

    # Sleep here for a few seconds to allow the service principal application to become active (ordinarily takes a few seconds)
    Sleep -s 15
    $NewRole = New-AzureRMRoleAssignment -RoleDefinitionName Contributor -ServicePrincipalName $Application.ApplicationId -ErrorAction SilentlyContinue
    $Retries = 0;
    While ($NewRole -eq $null -and $Retries -le 12)
    {
    Sleep -s 10
    New-AzureRMRoleAssignment -RoleDefinitionName Contributor -ServicePrincipalName $Application.ApplicationId | Write-Verbose -ErrorAction SilentlyContinue
    $NewRole = Get-AzureRMRoleAssignment -ServicePrincipalName $Application.ApplicationId -ErrorAction SilentlyContinue
    $Retries++;
    }
    return $Application.ApplicationId.ToString();
}

function CreateAutomationCertificateAsset ([string] $resourceGroup, [string] $automationAccountName, [string] $certifcateAssetName,[string] $certPath, [string] $certPlainPassword, [Boolean] $Exportable) {
    $CertPassword = ConvertTo-SecureString $certPlainPassword -AsPlainText -Force   
    Remove-AzureRmAutomationCertificate -ResourceGroupName $resourceGroup -AutomationAccountName $automationAccountName -Name $certifcateAssetName -ErrorAction SilentlyContinue
    New-AzureRmAutomationCertificate -ResourceGroupName $resourceGroup -AutomationAccountName $automationAccountName -Path $certPath -Name $certifcateAssetName -Password $CertPassword -Exportable:$Exportable  | write-verbose
}

function CreateAutomationConnectionAsset ([string] $resourceGroup, [string] $automationAccountName, [string] $connectionAssetName, [string] $connectionTypeName, [System.Collections.Hashtable] $connectionFieldValues ) {
    Remove-AzureRmAutomationConnection -ResourceGroupName $resourceGroup -AutomationAccountName $automationAccountName -Name $connectionAssetName -Force -ErrorAction SilentlyContinue
    New-AzureRmAutomationConnection -ResourceGroupName $ResourceGroup -AutomationAccountName $automationAccountName -Name $connectionAssetName -ConnectionTypeName $connectionTypeName -ConnectionFieldValues $connectionFieldValues
}

$ErrorActionPreference = "Stop"
$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition

try {   
    Push-Location
    cd $scriptPath    

    if ($SelfSignedCertPlainPassword -eq "") {
        Write-Error -Message "The certificate password can't be an empty string"
        return
    }

    Import-Module AzureRM.Profile
    Import-Module AzureRM.Resources

    $AzureRMProfileVersion= (Get-Module AzureRM.Profile).Version
    if (!(($AzureRMProfileVersion.Major -ge 2 -and $AzureRMProfileVersion.Minor -ge 1) -or ($AzureRMProfileVersion.Major -gt 2)))
    {
    Write-Error -Message "Please install the latest Azure PowerShell and retry. Relevant doc url : https://docs.microsoft.com/powershell/azureps-cmdlets-docs/ "
    return
    }

    Login-AzureRmAccount -EnvironmentName $EnvironmentName
    $Subscription = Select-AzureRmSubscription -SubscriptionId $SubscriptionId

    # Create the Resource group if it doesn't exist
	"Creating resource group $ResourceGroupName"
    New-AzureRmResourceGroup -Name $ResourceGroupName -Location $Location -Force 

    # Create the Automation Account if it doesn't exist
    $automationAccountName = $ResourceGroupName.Replace(" ", "").Replace("_", "").ToLowerInvariant() + "aa"	
	"Creating automation account $automationAccountName"
    New-AzureRmAutomationAccount -ResourceGroupName $ResourceGroupName -Location $Location -Name $automationAccountName -Plan Basic


    # Create a Run As account by using a service principal
    $CertifcateAssetName = "AzureRunAsCertificate"
    $ConnectionAssetName = "AzureRunAsConnection"
    $ConnectionTypeName = "AzureServicePrincipal"

    $CertificateName = $CertifcateAssetName
    $PfxCertPathForRunAsAccount = Join-Path $env:TEMP ($CertificateName + ".pfx")
    $PfxCertPlainPasswordForRunAsAccount = $SelfSignedCertPlainPassword
    $CerCertPathForRunAsAccount = Join-Path $env:TEMP ($CertificateName + ".cer")
	"Creating self signed certificate"
    CreateSelfSignedCertificate $CertificateName $PfxCertPlainPasswordForRunAsAccount $PfxCertPathForRunAsAccount $CerCertPathForRunAsAccount $SelfSignedCertNoOfMonthsUntilExpired

    # Create a service principal
	"Creating service principal"
    $PfxCert = New-Object -TypeName System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList @($PfxCertPathForRunAsAccount, $PfxCertPlainPasswordForRunAsAccount)
    $ApplicationId=CreateServicePrincipal $PfxCert $ApplicationDisplayName

    # Create the Automation certificate asset	
	"Creating Automation certificate asset"
    CreateAutomationCertificateAsset $ResourceGroupName $AutomationAccountName $CertifcateAssetName $PfxCertPathForRunAsAccount $PfxCertPlainPasswordForRunAsAccount $true

    # Populate the ConnectionFieldValues
    $SubscriptionId = $($Subscription.Subscription.SubscriptionId)
    $TenantId = $($Subscription.Tenant.TenantId)
    $Thumbprint = $PfxCert.Thumbprint
    $ConnectionFieldValues = @{"ApplicationId" = $ApplicationId; "TenantId" = $TenantId; "CertificateThumbprint" = $Thumbprint; "SubscriptionId" = $SubscriptionId}

     # Create an Automation connection asset named AzureRunAsConnection in the Automation account. This connection uses the service principal.	
	"Creating Automation connection asset"
    CreateAutomationConnectionAsset $ResourceGroupName $AutomationAccountName $ConnectionAssetName $ConnectionTypeName $ConnectionFieldValues

    # Log the results to the console
    "$($Subscription.Subscription.SubscriptionName) service principal"
    "***************************************************************************"
    " Application Name: $ApplicationDisplayName"
    " ApplicationId: $ApplicationId"
    " Tenant Id: $TenantId"
    " Certificate Thumbprint: $($PfxCert.Thumbprint)"
    " Subscription Id: $SubscriptionId"
    "***************************************************************************"
}
catch [Exception] {
  Write-Error "Error: $($_.Exception.Message)"
}
finally {
    Pop-Location
}
