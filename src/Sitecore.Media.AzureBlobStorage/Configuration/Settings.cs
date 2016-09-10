namespace Sitecore.Media.AzureBlobStorage.Configuration
{
    using System;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;

    public class Settings
    {
        public static class Media
        {
            public static class AzureBlobStorage
            {
                public static string StorageContainerName => GetSetting("Media.AzureBlobStorage.ContainerName");

                public static string StorageConnectionString => GetSetting("Media.AzureBlobStorage.ConnectionString");

                // Azure default: null
                // When NULL, it's set to 5 minutes
                public static TimeSpan? RequestServerTimeout => GetNullableTimeSpan("Media.AzureBlobStorage.RequestServerTimeout", TimeSpan.FromMinutes(5.0));

                // Max MaximunExecutionTime is TimeSpan.FromDays(24.0).
                // Default: null
                public static TimeSpan? MaximumExecutionTime => GetNullableTimeSpan("Media.AzureBlobStorage.MaximumExecutionTime", TimeSpan.FromDays(24.0));

                // Default: 1
                public static int ParallelOperationThreadCount => Sitecore.Configuration.Settings.GetIntSetting("Media.AzureBlobStorage.ParallelOperationThreadCount", 1);

                // Default: NoRetry
                public static IRetryPolicy RetryPolicy => GetRetryPolicy(Sitecore.Configuration.Settings.GetSetting("Media.AzureBlobStorage.RetryPolicy", "NoRetry"), TimeSpan.FromSeconds(3), 3);

                // Default: 3
                public static int RetryPolicyMaxAttempts => Sitecore.Configuration.Settings.GetIntSetting("Media.AzureBlobStorage.RetryPolicy.MaxAttempts", 3);

                // Default
                //      Exponential: 00:00:03
                //      Linear: 00:00:30
                public static TimeSpan? RetryPolicyDeltaBackoff => GetNullableTimeSpan("Media.AzureBlobStorage.RetryPolicy.DeltaBackoff", TimeSpan.FromSeconds(3.0));

                // Default: 32 MB
                public static long? SingleBlobUploadThresholdInBytes => Sitecore.Configuration.Settings.GetIntSetting("Media.AzureBlobStorage.SingleBlobUploadThresholdInBytes", 32 * 1024 * 1024);

                // Default: false
                public static bool UseTransactionalMD5 => Sitecore.Configuration.Settings.GetBoolSetting("Media.AzureBlobStorage.UseTransactionalMD5", false);

                public static bool StoreBlobContentMD5 => Sitecore.Configuration.Settings.GetBoolSetting("Media.AzureBlobStorage.StoreBlobContentMD5", false);

                // Default: 4 MB
                // Allowed between 16 KB and 4 MB
                public static int? StreamWriteSizeInBytes => GetNullableInt("Media.AzureBlobStorage.StreamWriteSizeInBytes", 4 * 1024 * 1024);

                // Default: Verbose
                public static LogLevel OperationContextLogLevel => GetLogLevel(Sitecore.Configuration.Settings.GetSetting("Media.AzureBlobStorage.OperationContext.LogLevel", "Verbose"));

            }
        }

        public static string GetSetting(string name)
        {
            return Sitecore.Configuration.Settings.GetSetting(name);
        }

        internal static TimeSpan? GetNullableTimeSpan(string settingName, TimeSpan defaultValue)
        {
            var settingValue = Sitecore.Configuration.Settings.GetTimeSpanSetting(settingName, defaultValue);
            if (settingValue.Equals(TimeSpan.Parse("00:00:00")))
            {
                return null;
            }

            return settingValue;
        }

        internal static int? GetNullableInt(string settingName, int defaultValue)
        {
            int? settingValue =
                Sitecore.Configuration.Settings.GetIntSetting("Media.AzureBlobStorage.StreamWriteSizeInBytes", defaultValue);
            return settingValue > 0 ? settingValue : null;
        }

        internal static IRetryPolicy GetRetryPolicy(string policyName, TimeSpan deltaBackoff, int maxAttempts)
        {
            IRetryPolicy policy;
            switch (policyName)
            {
                case "Exponential":
                    policy = new ExponentialRetry(deltaBackoff, maxAttempts);
                    break;
                case "Linear":
                    policy = new LinearRetry(deltaBackoff, maxAttempts);
                    break;
                case "NoRetry":
                    policy = new NoRetry();
                    break;
                default:
                    policy = new NoRetry();
                    break;
            }
            return policy;
        }

        internal static LogLevel GetLogLevel(string logLevelName)
        {
            LogLevel logLevel;
            bool success = Enum.TryParse(logLevelName, true, out logLevel);

            return success ? logLevel : LogLevel.Verbose;
        }
    }
}
