using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Processing;
using Sitecore.ExperienceForms.Processing.Actions;
using Sitecore.ExperienceForms.Data.Entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Stockpick.Form.Cloud.Model;
using Microsoft.WindowsAzure.Storage.Blob;
using Sitecore.DependencyInjection;
using Sitecore.ExperienceForms.Data;
using Microsoft.Azure.KeyVault;
using Stockpick.Form.Cloud.Crypto;
using System.Threading;

namespace Stockpick.Forms.Feature.ExperienceForms.Submit
{
    /// <summary>
    /// </summary>
    /// <seealso cref="Sitecore.ExperienceForms.Processing.Actions.SubmitActionBase{TParametersData}" />
    public class AzureQueueSubmit : SubmitActionBase<string>
    {
        private IFileStorageProvider _fileStorageProvider;

        private string _connectionstring;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogSubmit"/> class.
        /// </summary>
        /// <param name="submitActionData">The submit action data.</param>
        /// <param name="fileStorageProvider"></param>
        public AzureQueueSubmit(ISubmitActionData submitActionData) : base(submitActionData)
        {
            var stockpickFormsAzurequeueConnectionstring = "Stockpick.Forms.AzureQueue.Connectionstring";
            _connectionstring = Sitecore.Configuration.Settings.GetSetting(stockpickFormsAzurequeueConnectionstring);
            if (string.IsNullOrEmpty(_connectionstring))
            {
                Log.Warn("AzureQueueSubmit Forms configuration setting missing " + stockpickFormsAzurequeueConnectionstring, this);
            }
        }


        protected virtual IFileStorageProvider FileStorageProvider
        {
            get
            {
                return this._fileStorageProvider ?? (this._fileStorageProvider = ServiceLocator.ServiceProvider.GetService<IFileStorageProvider>());
            }
        }

        /// <summary>
        /// Tries to convert the specified <paramref name="value" /> to an instance of the specified target type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="target">The target object.</param>
        /// <returns>
        /// true if <paramref name="value" /> was converted successfully; otherwise, false.
        /// </returns>
        protected override bool TryParse(string value, out string target)
        {
            target = string.Empty;
            return true;
        }

        /// <summary>
        /// Executes the action with the specified <paramref name="data" />.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="formSubmitContext">The form submit context.</param>
        /// <returns>
        ///   <c>true</c> if the action is executed correctly; otherwise <c>false</c>
        /// </returns>
        protected override bool Execute(string data, FormSubmitContext formSubmitContext)
        {
            Assert.ArgumentNotNull(formSubmitContext, nameof(formSubmitContext));
            Assert.ArgumentNotNullOrEmpty(_connectionstring, nameof(_connectionstring));

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionstring);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container. use only lowercase!!
            CloudQueue queue = queueClient.GetQueueReference("stockpickformsqueue");

            // Create the queue if it doesn't already exist
            queue.CreateIfNotExists();

            // Create a message 
            var message = new FormFields
            {
                FormId = formSubmitContext.FormId.ToString(),
                Fields = new List<FormFieldSmall>()
            };
            List<Guid> filesource = new List<Guid>();
            foreach (var viewModel in formSubmitContext.Fields)
            {
                AddFieldData(viewModel, message);
                filesource.AddRange((IEnumerable<Guid>)GetCommittedFileIds(viewModel));
            }

            if (filesource.Any<Guid>())
            {
                StoreFiles(storageAccount, formSubmitContext, (IEnumerable<Guid>) filesource);
            }
            // Create a queue message with JSON and add it to the queue.
            CloudQueueMessage queuemessage = new CloudQueueMessage(JsonConvert.SerializeObject(message));
            queue.AddMessage(queuemessage);
            return true;
        }

        private  void StoreFiles(CloudStorageAccount storageAccount, FormSubmitContext formSubmitContext,IEnumerable<Guid> fileIds)
        {
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var blob = cloudBlobClient.GetContainerReference("stockpickformsblob");
            blob.CreateIfNotExists();

            var cloudResolver = new KeyVaultKeyResolver(Keyvault.GetToken);
            var url = Sitecore.Configuration.Settings.GetSetting("Stockpick.Forms.KeyFault.Key.URL");
            if (string.IsNullOrEmpty(url))
            {
                Log.Error("config key Stockpick.Forms.KeyFault.Key.URL is emty",this);
            }
            var key = cloudResolver.ResolveKeyAsync(url, CancellationToken.None).GetAwaiter().GetResult();
          
            foreach (var gui in fileIds)
            {
                var file = FileStorageProvider.GetFile(gui);
                if (file != null)
                {
                    Log.Info("Upload to Cloud storage file " + file.FileInfo.FileName + " as " + gui.ToString() + " using key kid:" + key.Kid, this);
              
                    BlobEncryptionPolicy policy = new BlobEncryptionPolicy(key, null);
                    BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = policy };

                    CloudBlockBlob cloudBlockBlob = blob.GetBlockBlobReference(gui.ToString());
                    using (var filestream = file.File)
                    {
                        cloudBlockBlob.UploadFromStream(filestream, null, options, null);
                    }

                }
            }

        }

        private static void AddFieldData(IViewModel viewModel, FormFields message)
        {
            var postedField = (IValueField) viewModel;
            IValueField valueField = postedField as IValueField;
            PropertyInfo property = postedField.GetType().GetProperty("Value");
            object postedValue =
                (object) property != null ? property.GetValue((object) postedField) : (object) null;
            property = postedField.GetType().GetProperty("Title");
            object postedTitle =
                (object) property != null ? property.GetValue((object) postedField) : (object) null;
            if (valueField.AllowSave && postedValue != null && postedTitle != null)
            {
                message.Fields.Add(new FormFieldSmall()
                {
                    Name = viewModel.Name,
                    Title = postedTitle.ToString(),
                    ItemId = viewModel.ItemId,
                    Value = ParseFieldValue(postedValue)
                });
            }
        }

        protected static string ParseFieldValue(object postedValue)
        {
            Assert.ArgumentNotNull(postedValue, nameof(postedValue));
            List<string> stringList = new List<string>();
            IList list = postedValue as IList;
            if (list != null)
            {
                foreach (object obj in (IEnumerable)list)
                    stringList.Add(obj.ToString());
            }
            else
                stringList.Add(postedValue.ToString());
            return string.Join(",", (IEnumerable<string>)stringList);
        }

        protected static IList<Guid> GetCommittedFileIds(IViewModel postedField)
        {
            Assert.ArgumentNotNull((object)postedField, nameof(postedField));
            List<Guid> committedFileIds = new List<Guid>();
            IValueField valueField = postedField as IValueField;
            if (valueField == null || !valueField.AllowSave)
                return (IList<Guid>)committedFileIds;
            PropertyInfo property = postedField.GetType().GetProperty("Value");
            object obj = (object)property != null ? property.GetValue((object)postedField) : (object)null;
            if (obj == null)
                
                return (IList<Guid>)committedFileIds;
            (obj as List<StoredFileInfo>)?.ForEach((Action<StoredFileInfo>)(fileInfo => committedFileIds.Add(fileInfo.FileId)));
            return (IList<Guid>)committedFileIds;
        }

    }
}

