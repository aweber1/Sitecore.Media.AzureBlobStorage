namespace Sitecore.Media.AzureBlobStorage.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Data.DataProviders;
    using Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;

    [TestClass]
    public class AzureBlobStorageProviderTests
    {
        public const string ContainerName = "sitecore-media-library";
        public const string ConnString = "UseDevelopmentStorage=true";
        public const string MediaFilePath = @"c:\temp\test.pdf";
        public ICloudStorageProvider StorageProvider;
        public List<string> mediaList;

        [TestInitialize]
        public void Setup()
        {
            StorageProvider = new AzureBlobStorageProvider(ConnString, ContainerName);
            mediaList = new List<string>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var mediaId in mediaList)
            {
                var success = StorageProvider.Delete(mediaId);
                System.Diagnostics.Debug.Print($"Blob with id {mediaId} {(success ? "was" : "was not")} deleted");
            }
        }

        [TestMethod]
        public void Put_Media_Into_Storage()
        {
            // Arrange
            var azStorageProvider = new AzureBlobStorageProvider(ConnString, ContainerName);
            var fileStream = File.OpenRead(MediaFilePath);
            var mediaId = GetMediaId();
            mediaList.Add(mediaId);

            // Act
            azStorageProvider.Put(fileStream, mediaId);

            // Assert
            Assert.AreEqual(mediaId, azStorageProvider.GetBlobReference(mediaId)?.Name);
        }

        [TestMethod]
        public void Get_Media_From_Storage()
        {
            // Arrange
            var fileStream = File.OpenRead(MediaFilePath);
            var mediaId = GetMediaId();
            StorageProvider.Put(fileStream, mediaId);

            var stream = new MemoryStream();

            // Act
            StorageProvider.Get(stream, mediaId);

            // Assert
            System.Diagnostics.Debug.Print($"Stream length {stream.Length}");
            Assert.IsTrue(stream.Length > 0);
        }

        [TestMethod]
        public void Media_Exists_Check()
        {
            var fileStream = File.OpenRead(MediaFilePath);
            var mediaId = GetMediaId();
            StorageProvider.Put(fileStream, mediaId);

            var exists = StorageProvider.Exists(mediaId);

            Assert.IsTrue(exists);

        }

        [TestMethod]
        public void Media_Not_Exists_Check()
        {
            var exists = StorageProvider.Exists("fake_id");

            Assert.IsFalse(exists);

        }

        [TestMethod]
        public void Delete_Media_From_Storage()
        {
            var fileStream = File.OpenRead(MediaFilePath);
            var mediaId = GetMediaId();
            StorageProvider.Put(fileStream, mediaId);

            var success = StorageProvider.Delete(mediaId);

            Assert.IsTrue(success);
        }

        private string GetMediaId()
        {
            var mediaId = mediaList.Any() ? mediaList.First() : Guid.NewGuid().ToString();
            if (!mediaList.Contains(mediaId)) { mediaList.Add(mediaId); }
            return mediaId;
        }
    }
}
