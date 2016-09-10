namespace Sitecore.Media.AzureBlobStorage.Helpers
{
    using Diagnostics;

    static class LogHelper
    {
        public static void LogDebugInfo(string message)
        {
            if (Log.IsDebugEnabled && !Sitecore.Context.IsUnitTesting)
            {
                Log.Debug(message);
            }
        }
    }
}
