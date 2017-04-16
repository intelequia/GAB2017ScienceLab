![Global Azure Bootcamp 2017 - Science Lab](https://globalazurebootcamp.blob.core.windows.net/images/Pano-ORM-perfil-Lactea-DLopez-779x389.jpg)

# Introduction
This project contains all the source code for the Global Azure Bootcamp 2017 Science Lab. Created by David Rodriguez ([@davidjrh](http://twitter.com/davidjrh)), Adonai Suárez ([@adonaisg](http://twitter.com/adonaisg)), 
Martin Abbott ([@martinabbott](http://twitter.com/martinabbott)) and Wesley Cabus ([@WesleyCabus](http://twitter.com/wesleycabus)) for the Global Azure Bootcamp 2017 Science Lab running Sebastian Hidalgo's Star 
Formation History SELIGA algorithm at the [Instituto de Astrofisica de Canarias](http://www.iac.es/index.php?lang=en).

See more at http://global.azurebootcamp.net 

# Getting Started
Follow these steps to deploy the GAB Science Lab:

## Requirements

In order to participate on the GAB Science Lab you will need:
* An active Azure subscription. You will need to login as owner of the subscription to setup the needed credentials. You can signup for a free subscription [here](https://azure.microsoft.com/free/).
* Available quota for the following resources:
 * 1 Batch Account for the deployment location (you can't have more than 1 batch account per location)
 * 1 core available (you will need to check the core quotas for the subscription and batch accounts)


## Installation process

### Step 1. Download the GAB Science lab scripts
Download from [this location](https://globalazurebootcamp.blob.core.windows.net/release/GABLab.zip) the Science Lab scripts and unzip them in a local folder. You will see the following files:
* Setup-GABLabAccount.ps1
* Deploy-GABLabClient.ps1
* GABClient.json
* GABClient.parameters.json

### Step 2. Setup Azure Automation credentials
The GAB Science Lab uses Azure Automation to feed the Azure Batch service with inputs to process. In order to automate the process, an Application must 
be setup as Service Principal. To make this process easier, follow these steps:

Requisites:
* **Windows 10 is needed to run this step**. If you don't have Windows 10, ask a friend or your local event staff to help you on this one.
* Install de latest version of Microsoft Azure PowerShell through the Web Platform Installer or downloading from [here](https://www.microsoft.com/web/handlers/webpi.ashx/getinstaller/WindowsAzurePowershellGet.3f.3f.3fnew.appids).


1. Open a PowerShell console **as Administrator**
2. Change the scripts execution policy to Unrestricted by executing:
```PowerShell
    Set-ExecutionPolicy -ExecutionPolicy Unrestricted
```
3. Execute the Setup-GABLabAccount.ps1 script. These parameters are required:
    * **SubscriptionId**: The Id of the subscription you want use to deploy the Lab Client. Follow [this link](https://blogs.msdn.microsoft.com/mschray/2016/03/18/getting-your-azure-subscription-guid-new-portal/) to get your Azure Subscription Id
    * **ResourceGroupName**: The name of the Resource Group that will be created. (e.g. GABLab2017)
    * **Location**: An Azure Location where the Resource Group will be created. The resource group will contain, between other resources an Automation Account that is not available yet in all the locations. **IMPORTANT: You need to specify one of these locations:**
        "East US 2"
        "South Central US"
        "West Central US"
        "North Europe"
        "West Europe"
        "Southeast Asia"
        "Japan East"
        "Australia Southeast"
        "Central India"
        "Canada Central"
        "UK South" 
        The lab will use an Azure Batch account provisioned on Step 3. Note that you can provision only ONE Azure Batch account per location
    * **ApplicationDisplayName**: The name of the Application (e.g. GABLab2017)
    * **SelfSignedPlainPassword**: A plain text password for the Self-signed Certificate. Ensure to use double quotes enclosing the password, and don't use an empty password or the script will fail
```PowerShell
.\Setup-GABLabAccount.ps1 -SubscriptionId "d159cc75-d0ef-4d44-834f-9ed08567ac31" -ResourceGroupName "GABLab2017" -Location "North Europe" -ApplicationDisplayName "GABLab2017" -SelfSignedCertPlainPassword "abc"
```
4. A credential login prompt will be appear. You must login with your Azure credentials.

If the process finished without errors, this step is completed.

### Step 3. Deploy the GAB Science Lab

There are two ways to do this step.

#### The easy way: Azure portal (recommended)

1. Click on the button: [![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/http%3A%2F%2Fglobalazurebootcamp.blob.core.windows.net%2Frelease%2Fassets%2FGABClient.json)
2. Fill the form. You can get info about each field if you hold the cursor over the info icon.
	**IMPORTANT: you must select the existing subscription and Resource Group you specified on Step 2.**

Now relax and wait for the green check. Will take around 10 minutes to complete.

![Deployment Complete](https://globalazurebootcamp.blob.core.windows.net/images/DeploymentComplete.png)
	
#### The other way: continue using PowerShell

1. Open GABClient.parameters.json with a text editor.
2. Change the value of the parameters. Before do this, read the next notes:
    * **Email, FullName, TeamName, CompanyName**: fill with your personal info. It be displayed on the global dashboards.
    * **CountryCode**: the 2 character ISO2 country code. Find your code at [Wikipedia](https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2)
    * **LabKeyCode**: Is a predefined string with your location LAB Key. Ask admin staff at your location for the code.    
    * **VirtualMachinesSku**: Size of the virtual machines for the Azure Batch pool. We recommend select one of these (1 core Skus available for batch pools):
        * Standard_A1        
        * Standard_D1
        * Standard_D1_v2
        * Standard_F1
    * **InstanceCount**: Number of virtual machine instances (100 or less). Check the available cores in your subscription before setting up a big number.
    You can start with 1 or 2 virtual machines and change the number of them later on the portal.
3. Execute the Deploy-GABLabClient.ps1 script in a PowerShell. These parameters are required:
    * **ResourceGroupName**: use the **SAME resource group name** you used on step 2 when setting up the GAB Lab account
	* **ResourceGroupLocation**: use the **SAME location** you used on step 2 when setting up the GAB Lab account
    ```PowerShell
    .\Deploy-GABLabClient.ps1 -ResourceGroupName "GABLab2017" -ResourceGroupLocation "North Europe"
    ```

Now wait for the deployment. After around 10 minutes, you should start seeing the things happening on the batch account.

# Verifying that the lab is working
Once the lab has been deployed, you will see a set of resources under the resource group:
### An Azure Storage account (needed by Azure Batch)

![Check Azure Storage account](https://globalazurebootcamp.blob.core.windows.net/images/check-deploy-1.png)

Your Resource Group must be similar to this screenshot. It must have an Azure Storage account, an Azure Automation account, an Azure Batch account and 3 runbooks.

### An Azure Automation account (and the runbooks)

![Check Automation account](https://globalazurebootcamp.blob.core.windows.net/images/check-deploy-2.png)

Check if there are 3 runbooks, 36 assets and the status of the automation account is active. 

![Check Automation account jobs](https://globalazurebootcamp.blob.core.windows.net/images/check-deploy-4.png)

Check if the runbook Create-BatchPool and Setup-JobSchedules have been executed and their status is Completed (these only run one time during the initial deployment). In this screen, you 
can also see a few runs of the Get-NewBatch runbook. This one runs every 5min to gather new inputs to process. 

### An Azure Batch account

![Check Batch account](https://globalazurebootcamp.blob.core.windows.net/images/check-deploy-7.png)

Open your Azure Batch account and check if one pool of nodes was created.

![Check Batch account](https://globalazurebootcamp.blob.core.windows.net/images/check-deploy-6.png)

In the pool sumary, you will see one square for each node you have deployed. On the right, you can see the status guide of the nodes. Probably, your nodes will be in RUNNING state, 
depending on when you are checking this. Maybe, they are in state IDLE, CREATING, STARTING or WAITING FOR STAR TASK. It is correct. But, if the state is START TASK FAILED, OFFLINE, 
UNKNOWN or UNUSABLE, something is wrong with the deployment. In that case, please contact your local staff.

# Let's play

Well, since the lab is working correctly, you maybe want play with it. Here are some things you can try:

## Scale your Azure Batch Pool

Do you want to appear in the Global Dashboard? Then do it by donating more computing time! ;) If you increase the number of VMs running tasks, the probability to improve your scores is higher. So, go to the Azure Portal and scale your Azure Batch pool:

![Scale Azure Batch pool](https://globalazurebootcamp.blob.core.windows.net/images/scale-pool-1.png)

Go to the overview tab of you Azure Batch pool and click "Scale" button on the top.

![Scale Azure Batch pool](https://globalazurebootcamp.blob.core.windows.net/images/scale-pool-2.png)

Now, fill the form:
* **Mode:** Ensure that the selected mode is Fixed.
* **Target dedicated:** Set the number of nodes after the scale. Keep in mind the availabilty of cores in your subscription (step 3). You can't normally go higher than 20 without contacting Microsoft support, 
that normally takes some working days, so please, instead of filling MS support inbox with quota requests, if you want to deploy more than 20 cores, try to deploy another GAB Science lab instance in another location.
* **Current task action:** Probably, your nodes are running a task, ensure that "Requeue" is selected. With this option, the data of the task will not be lost.
* **Resize timeout:** You can use the default value.

# Decomissioning the Science Lab
Your lab deployment will continue working processing inputs until April 23rd 23:59 or until you delete the deployment resources. Of course, you don't need to wait until that time, but the lab won't continue gathering inputs
after that date. In order to delete your deployment:
1. Select the Resource Group containing your science lab deployment
2. Click on Delete and confirm by typing your resource group name

Thanks for your support on Global Azure Bootcamp 2017 Science Lab. Live Long and Prosper!

# Frequently Asked Questions
1. **How much will cost?**

The lab uses an Azure Automation Basic account and a Batch Account. The cost of Automation Account would be less than $1 for a full day. The Azure Batch service is free, Microsoft only charges for the underlying Virtual Machines used on the batch pool. 
So for example, if you deploy the science lab with 2 instances of A1 Linux VMs during 12 hours, the costs would be under $2.
For more information about pricing:
* [Linux Virtual Machines](https://azure.microsoft.com/en-us/pricing/details/virtual-machines/linux/)
* [Batch](https://azure.microsoft.com/en-us/pricing/details/batch/)
* [Automation](https://azure.microsoft.com/en-us/pricing/details/automation/)

# Known issues
1. **When deploying the lab you get a "Conflict" error on the CreateBatchPoolJob step**
Check that the selected VMs are available in your subscription for the desired location. Also check that you have remaining cores available on your subscription. We have observed that in some locations, 
certain VM Skus are not available for batch. Deploying A1 or D1 VMs should work everywhere. As a workaround, you can go and manually run the runbook "Create-BatchPool" with a different VM Sku
