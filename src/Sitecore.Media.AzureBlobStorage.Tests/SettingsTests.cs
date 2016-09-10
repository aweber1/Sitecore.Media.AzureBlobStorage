using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sitecore.Media.AzureBlobStorage.Tests
{
    using Configuration;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;

    [TestClass]
    public class SettingsTests
    {
        [TestMethod]
        public void Get_LogLevel_Verbose()
        {
            // Arrange
            // Act
            LogLevel logLevel = Settings.GetLogLevel("Verbose");

            // Assert
            Assert.AreEqual(logLevel, LogLevel.Verbose);
        }

        [TestMethod]
        public void Get_LogLevel_Error()
        {
            // Arrange
            // Act
            LogLevel logLevel = Settings.GetLogLevel("Error");

            // Assert
            Assert.AreEqual(logLevel, LogLevel.Error);
        }

        [TestMethod]
        public void Get_LogLevel_Informational()
        {
            // Arrange
            // Act
            LogLevel logLevel = Settings.GetLogLevel("Informational");

            // Assert
            Assert.AreEqual(logLevel, LogLevel.Informational);
        }

        [TestMethod]
        public void Get_RetryPolicy_NoRetry()
        {
            // Arrange
            // Act
            IRetryPolicy policy = Settings.GetRetryPolicy("NoRetry", TimeSpan.Zero, 0);

            // Assert
            Assert.AreEqual(policy.GetType(), typeof(NoRetry));
        }

        [TestMethod]
        public void Get_RetryPolicy_Linear()
        {
            // Arrange
            // Act
            IRetryPolicy policy = Settings.GetRetryPolicy("Linear", TimeSpan.FromSeconds(30), 3);

            // Assert
            Assert.AreEqual(policy.GetType(), typeof(LinearRetry));
        }

        [TestMethod]
        public void Get_RetryPolicy_Exponential()
        {
            // Arrange
            // Act
            IRetryPolicy policy = Settings.GetRetryPolicy("Exponential", TimeSpan.FromSeconds(3), 3);

            // Assert
            Assert.AreEqual(policy.GetType(), typeof(ExponentialRetry));
        }
    }
}
