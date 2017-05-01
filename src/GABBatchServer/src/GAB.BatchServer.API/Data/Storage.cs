using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GAB.BatchServer.API.Data
{
    /// <summary>
    /// Utility class for Azure Storage management
    /// </summary>
    public static class Storage
    {
        /// <summary>
        /// Inputs container
        /// </summary>
        public static CloudBlobContainer InputsContainer { get; set; }
        /// <summary>
        /// Ouputs container
        /// </summary>
        public static CloudBlobContainer OutputsContainer { get; set; }

        /// <summary>
        /// Initializes the storage account creating the containers
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="seedData"></param>
        public static async void Initialize(IConfiguration configuration, ILogger logger, bool seedData)
        {
            logger.LogInformation("Initializing Azure storage");
            var account = CloudStorageAccount.Parse(configuration.GetConnectionString("Storage"));
            var client = account.CreateCloudBlobClient();
            
            var inputsContainerName = configuration["BatchServer:InputsContainerName"];
            logger.LogInformation($"Creating inputs container '{inputsContainerName}'");
            InputsContainer = client.GetContainerReference(inputsContainerName);
            await InputsContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, null, null)
                .ConfigureAwait(continueOnCapturedContext: false);

            var outputsContainerName = configuration["BatchServer:OutputsContainerName"];
            logger.LogInformation($"Creating inputs container '{outputsContainerName}'");
            OutputsContainer = client.GetContainerReference(outputsContainerName);
            await OutputsContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null)
                .ConfigureAwait(continueOnCapturedContext: false);

            logger.LogInformation($"Storage successfully initialized");
        }

        /// <summary>
        /// Uploads a output result to the storage account
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="blob"></param>
        /// <param name="contents"></param>
        /// <returns></returns>
        public static async Task UploadOutputAsync(IConfiguration configuration, CloudBlockBlob blob, string contents)
        {
            var account = CloudStorageAccount.Parse(configuration.GetConnectionString("Storage"));
            var client = account.CreateCloudBlobClient();

            var options = new BlobRequestOptions
            {
                RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 10)
            };
            ((CloudBlob) blob).Properties.ContentType = "text/plain";
            await blob.UploadTextAsync(contents, null, options, null);
        }
    }
}
