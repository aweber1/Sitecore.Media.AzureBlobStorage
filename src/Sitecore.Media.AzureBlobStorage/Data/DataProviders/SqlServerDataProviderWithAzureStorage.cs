namespace Sitecore.Media.AzureBlobStorage.Data.DataProviders
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using Collections;
    using Diagnostics;
    using Interfaces;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.DataProviders;
    using Sitecore.Data.Managers;
    using Sitecore.Data.SqlServer;
    using Settings = Configuration.Settings;

    public class SqlServerDataProviderWithAzureStorage : SqlServerDataProvider
    {
        private readonly LockSet _blobSetLocks;
        public readonly ICloudStorageProvider StorageProvider;
        //private CloudStorageAccount _storageAccount;
        //private CloudBlobClient _blobClient;
        //private CloudBlobContainer _blobContainer;

        //protected CloudStorageAccount StorageAccount
        //{
        //    get { return _storageAccount ?? (_storageAccount = CloudStorageAccount.Parse(Settings.Media.AzureBlobStorage.StorageConnectionString)); }
        //}

        //protected CloudBlobClient BlobClient => _blobClient ?? (_blobClient = StorageAccount.CreateCloudBlobClient());

        //protected CloudBlobContainer BlobContainer
        //{
        //    get
        //    {
        //        if (_blobContainer == null)
        //        {
        //            _blobContainer = BlobClient.GetContainerReference(Settings.Media.AzureBlobStorage.StorageContainerName);
        //            _blobContainer.CreateIfNotExists(BlobContainerPublicAccessType.Container);
        //        }
        //        return _blobContainer;
        //    }
        //}

        public SqlServerDataProviderWithAzureStorage(string connectionString) : base(connectionString)
        {
            _blobSetLocks = new LockSet();
            StorageProvider = new AzureBlobStorageProvider(Settings.Media.AzureBlobStorage.StorageConnectionString, Settings.Media.AzureBlobStorage.StorageContainerName);
        }

        public override Stream GetBlobStream(Guid blobId, CallContext context)
        {
            Assert.ArgumentNotNull(context, "context");

            
            //var blob = BlobContainer.GetBlockBlobReference(blobId.ToString());
            //if (!blob.Exists())
            //    return null;

            var memStream = new MemoryStream();
            //blob.DownloadToStream(memStream);
            StorageProvider.Get(memStream, blobId.ToString());
            return memStream.Length > 0 ? memStream : null;
        }

        public override bool BlobStreamExists(Guid blobId, CallContext context)
        {
            //var blob = BlobContainer.GetBlockBlobReference(blobId.ToString());
            //return blob.Exists();
            return StorageProvider.Exists(blobId.ToString());
        }

        public override bool RemoveBlobStream(Guid blobId, CallContext context)
        {
            //var blob = BlobContainer.GetBlockBlobReference(blobId.ToString());
            //blob.DeleteIfExists();
            //return base.RemoveBlobStream(blobId, context);
            return StorageProvider.Delete(blobId.ToString());
        }

        public override bool SetBlobStream(Stream stream, Guid blobId, CallContext context)
        {
            lock (_blobSetLocks.GetLock(blobId))
            {
                try
                {
                    //var blob = BlobContainer.GetBlockBlobReference(blobId.ToString());

                    //SqlServer SetBlobStream method deletes existing blob before inserting, does UploadFromStream overwrite existing blob?
                    //blob.UploadFromStream(stream);
                    StorageProvider.Put(stream, blobId.ToString());

                    //insert an empty reference to the BlobId into the SQL Blobs table, this is basically to assist with the cleanup process.
                    //during cleanup, it's faster to query the database for the blobs that should be removed as opposed to retrieving and parsing a list from Azure.
                    const string cmdText =
                        "INSERT INTO [Blobs]( [Id], [BlobId], [Index], [Created], [Data] ) VALUES(   NewId(), @blobId, @index, @created, @data)";
                    using (var connection = new SqlConnection(Api.ConnectionString))
                    {
                        connection.Open();
                        var command = new SqlCommand(cmdText, connection)
                        {
                            CommandTimeout = (int) CommandTimeout.TotalSeconds
                        };
                        command.Parameters.AddWithValue("@blobId", blobId);
                        command.Parameters.AddWithValue("@index", 0);
                        command.Parameters.AddWithValue("@created", DateTime.UtcNow);
                        command.Parameters.Add("@data", SqlDbType.Image, 0).Value = new byte[0];
                        command.ExecuteNonQuery();
                    }
                }
                catch (StorageException ex)
                {
                    Log.Error($"Upload of blob with Id {blobId} failed.", ex, this);
                    throw;
                }
            }
            return true;
        }

        protected override void CleanupBlobs(CallContext context)
        {
            Factory.GetRetryer().ExecuteNoResult(() => DoCleanup(context));
        }

        //the majority of the code for the CleanupBlobs process is from the default SQL Server Data Provider
        protected virtual void DoCleanup(CallContext context)
        {
            IEnumerable<Guid> blobsToDelete;

            using (var transaction = Api.CreateTransaction())
            {
                const string blobsInUseTempTableName = "#BlobsInUse";
                Api.Execute("CREATE TABLE {0}" + blobsInUseTempTableName + "{1} ({0}ID{1} {0}uniqueidentifier{1})", new object[0]);

                var blobsInUse = GetBlobsInUse(context.DataManager.Database);

                foreach (var blobReference in blobsInUse)
                {
                    Api.Execute("INSERT INTO {0}" + blobsInUseTempTableName + "{1} VALUES ({2}id{3})", new object[] { "id", blobReference });
                }

                blobsToDelete = GetUnusedBlobs("#BlobsInUse");
                
                const string sql = " DELETE\r\n FROM {0}Blobs{1}\r\n WHERE {0}BlobId{1} NOT IN (SELECT {0}ID{1} FROM {0}" + blobsInUseTempTableName + "{1})";
                Api.Execute(sql, new object[0]);
                Api.Execute("DROP TABLE {0}" + blobsInUseTempTableName + "{1}", new object[0]);
                transaction.Complete();
            }

            foreach (var blobId in blobsToDelete)
            {
                //var blob = BlobContainer.GetBlockBlobReference(blobId.ToString());
                //blob.DeleteIfExists();
                StorageProvider.Delete(blobId.ToString());
            }
        }

        //note: items in the recycle bin are technically still referenced/in use, so be sure to empty the recycle bin before attempting clean up blobs
        protected virtual IEnumerable<Guid> GetBlobsInUse(Database database)
        {
            var tables = new[] { "SharedFields", "UnversionedFields", "VersionedFields", "ArchivedFields" };
            var blobsInUse = new List<Guid>();
            foreach (var template in TemplateManager.GetTemplates(database).Values)
            {
                foreach (var field in template.GetFields())
                {
                    if (!field.IsBlob)
                        continue;

                    foreach (var sql in tables.Select(table => "SELECT DISTINCT {0}Value{1}\r\n FROM {0}" + table + "{1}\r\n WHERE {0}FieldId{1} = {2}fieldId{3}\r\n AND {0}Value{1} IS NOT NULL \r\n AND {0}Value{1} != {6}"))
                    {
                        using (var reader = Api.CreateReader(sql, new object[] { "fieldId", field.ID }))
                        {
                            while (reader.Read())
                            {
                                var id = Api.GetString(0, reader);
                                if (id.Length > 38)
                                {
                                    id = id.Substring(0, 38);
                                }
                                ID parsedId;
                                if (ID.TryParse(id, out parsedId))
                                {
                                    blobsInUse.Add(parsedId.Guid);
                                }
                            }
                        }
                    }
                }
            }
            return blobsInUse;
        }
        
        protected virtual IEnumerable<Guid> GetUnusedBlobs(string blobsInUseTempTableName)
        {
            var unusedBlobs = new List<Guid>();
            //this database call is dependent on the #BlobsInUse temporary table
            var sql = "SELECT {0}BlobId{1}\r\n FROM {0}Blobs{1}\r\b WHERE {0}BlobId{1} NOT IN (SELECT {0}ID{1} FROM {0}" + blobsInUseTempTableName + "{1})";
            using (var reader = Api.CreateReader(sql, new object[0]))
            {
                while (reader.Read())
                {
                    var id = Api.GetGuid(0, reader);
                    unusedBlobs.Add(id);
                }
            }
            return unusedBlobs;
        }
    }
}
