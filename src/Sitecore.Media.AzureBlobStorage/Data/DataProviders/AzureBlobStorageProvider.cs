namespace Sitecore.Media.AzureBlobStorage.Data.DataProviders
{
    using System.IO;
    using Diagnostics;
    using Helpers;
    using Interfaces;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public class AzureBlobStorageProvider : ICloudStorageProvider
    {
        protected CloudStorageAccount StorageAccount { get; set; }

        protected CloudBlobClient BlobClient { get; set; }

        protected CloudBlobContainer BlobContainer { get; set; }

        #region ctor

        public AzureBlobStorageProvider(string blobStorageConnectionString, string storageContainerName)
        {
            StorageAccount = CloudStorageAccount.Parse(blobStorageConnectionString);
            BlobClient = StorageAccount.CreateCloudBlobClient();
            BlobContainer = CreateBlobContainer(BlobClient, storageContainerName);
        }

        #endregion ctor

        #region ICloudStorageProvider members

        public void Put(Stream stream, string blobId)
        {
            // Use BlockBlob to store media assets
            // If blob with such Id already exists, it will be overwritten
            var blob = BlobContainer.GetBlockBlobReference(blobId);

            // Set blob streaming chunks if file larger than value specified in SingleBlobUploadThresholdInBytes setting.
            var streamWriteSizeBytes = Configuration.Settings.Media.AzureBlobStorage.StreamWriteSizeInBytes;
            if (streamWriteSizeBytes.HasValue && streamWriteSizeBytes.Value > 0)
            {
                blob.StreamWriteSizeInBytes = streamWriteSizeBytes.Value;
            }

            var requestProperties = GetRequestProperties();
            var timer = new HighResTimer(true);
            //SqlServer SetBlobStream method deletes existing blob before inserting, does UploadFromStream overwrite existing blob?
            blob.UploadFromStream(stream, options: requestProperties.Item1, operationContext: requestProperties.Item2);
            timer.Stop();
            LogHelper.LogDebugInfo($"Upload of blob {blobId} of size {stream.Length} took {timer.ElapsedTimeSpan.ToString(Configuration.Constants.TimeSpanDebugFormat)}");

#if DEBUG
            var requestsInfo = new System.Text.StringBuilder();
            foreach (var request in requestProperties.Item2.RequestResults)
            {
                requestsInfo.AppendLine($"{request.ServiceRequestID} - {request.HttpStatusCode} - {request.HttpStatusMessage} - bytes: {request.EgressBytes}");
            }
            Log.Info($"operationContext.RequestResults: {requestsInfo}", this);
#endif
        }

        public void Get(Stream target, string blobId)
        {
            var blob = GetBlobReference(blobId);
            if (blob.Exists())
            {
                var requestProperties = GetRequestProperties();
                var timer = new HighResTimer(true);
                blob.DownloadToStream(target: target, options: requestProperties.Item1,
                    operationContext: requestProperties.Item2);
                timer.Stop();
                LogHelper.LogDebugInfo(
                    $"Download of blob with Id {blobId} of size {target.Length} took {timer.ElapsedTimeSpan.ToString(Configuration.Constants.TimeSpanDebugFormat)}");
            }
        }

        public bool Delete(string blobId)
        {
            var blob = GetBlobReference(blobId);
            var requestProperties = GetRequestProperties();
            var success = blob.DeleteIfExists(options: requestProperties.Item1, operationContext: requestProperties.Item2);
            LogHelper.LogDebugInfo($"Blob with Id {blobId} {(success ? "was" : "was not")} deleted");
            if (!success)
            {
                LogHelper.LogDebugInfo($"Delete operation context StatusCode: {requestProperties.Item2.LastResult?.HttpStatusCode}, StatusMessage: {requestProperties.Item2.LastResult?.HttpStatusMessage}, ErrorMessage: {requestProperties.Item2.LastResult?.ExtendedErrorInformation?.ErrorMessage}");
            }
            return success;
        }

        public bool Exists(string blobId)
        {
            var blob = BlobContainer.GetBlobReference(blobId);
            return blob.Exists();
        }

        #endregion ICloudStorageProvider members

        internal virtual CloudBlob GetBlobReference(string blobId)
        {
            return BlobContainer.GetBlobReference(blobId);
        }

        protected virtual System.Tuple<BlobRequestOptions, OperationContext> GetRequestProperties()
        {
            var requestOptions = new BlobRequestOptions()
            {
                MaximumExecutionTime = Configuration.Settings.Media.AzureBlobStorage.MaximumExecutionTime,
                ParallelOperationThreadCount = Configuration.Settings.Media.AzureBlobStorage.ParallelOperationThreadCount,
                RetryPolicy = Configuration.Settings.Media.AzureBlobStorage.RetryPolicy,
                ServerTimeout = Configuration.Settings.Media.AzureBlobStorage.RequestServerTimeout,
                SingleBlobUploadThresholdInBytes = Configuration.Settings.Media.AzureBlobStorage.SingleBlobUploadThresholdInBytes,
                StoreBlobContentMD5 = Configuration.Settings.Media.AzureBlobStorage.StoreBlobContentMD5,
                UseTransactionalMD5 = Configuration.Settings.Media.AzureBlobStorage.UseTransactionalMD5
            };

            var operationContext = new OperationContext()
            {
                LogLevel = Configuration.Settings.Media.AzureBlobStorage.OperationContextLogLevel
            };

            
            return System.Tuple.Create(requestOptions, operationContext);
        }

        private CloudBlobContainer CreateBlobContainer(CloudBlobClient blobClient, string storageContainerName)
        {
            var container = blobClient.GetContainerReference(storageContainerName);
            container.CreateIfNotExists();
            return container;
        }
    }
}
