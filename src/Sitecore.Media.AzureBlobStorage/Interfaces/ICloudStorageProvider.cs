namespace Sitecore.Media.AzureBlobStorage.Interfaces
{
    using System;
    using System.IO;

    public interface ICloudStorageProvider
    {
        void Put(Stream stream, string blobId);
        void Get(Stream target, string blobId);
        bool Delete(string blobId);
        bool Exists(string blobId);
    }
}
