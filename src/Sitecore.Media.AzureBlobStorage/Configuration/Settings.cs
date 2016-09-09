namespace Sitecore.Media.AzureBlobStorage.Configuration
{
	public class Settings
	{
		public static class Media
		{
			public static class AzureBlobStorage
			{
				public static string StorageContainerName
				{
					get { return GetSetting("Media.AzureBlobStorage.ContainerName"); }
				}

				public static string StorageConnectionString
				{
					get { return GetSetting("Media.AzureBlobStorage.ConnectionString"); }
				}
			}
		}

		public static string GetSetting(string name)
		{
			return Sitecore.Configuration.Settings.GetSetting(name);
		}
	}
}
