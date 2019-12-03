using Microsoft.WindowsAzure.Storage.Blob;
using System.Web.Mvc;
using Microsoft.Azure.KeyVault;
using Stockpick.Form.Cloud.Crypto;
using System.Threading;
using Sitecore.Extensions;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Sitecore.Diagnostics;
using System.IO;
using Sitecore.Mvc.Controllers;

namespace DecryptBlobFile.Controllers
{
    public class DecryptController : SitecoreController
    {
        [System.Web.Http.HttpGet, System.Web.Http.HttpPost]
        public FileStreamResult File(string filename)
        {
            var stockpickFormsAzurequeueConnectionstring = "Stockpick.Forms.AzureQueue.Connectionstring";
            var connectionstring = Sitecore.Configuration.Settings.GetSetting(stockpickFormsAzurequeueConnectionstring);

            Microsoft.WindowsAzure.Storage.CloudStorageAccount storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(connectionstring);

            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = cloudBlobClient.GetContainerReference("stockpickformsblob");
            blobContainer.CreateIfNotExists();

            var cloudResolver = new KeyVaultKeyResolver(Keyvault.GetToken);
            var url = Sitecore.Configuration.Settings.GetSetting("Stockpick.Forms.KeyFault.Key.URL");
            if (string.IsNullOrEmpty(url))
            {
                Log.Error("config key Stockpick.Forms.KeyFault.Key.URL is emty", this);
            }
            var key = cloudResolver.ResolveKeyAsync(url, CancellationToken.None).GetAwaiter().GetResult();

            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(filename);


            BlobEncryptionPolicy policy = new BlobEncryptionPolicy(null, cloudResolver);
            BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = policy };
            
            Stream blobStream = blob.OpenRead(null,options);
            return new FileStreamResult(blobStream, "application/x-binary");
        }
    }
}