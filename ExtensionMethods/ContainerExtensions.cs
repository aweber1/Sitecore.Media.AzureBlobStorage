using Microsoft.WindowsAzure.StorageClient;

namespace Sitecore.AzureExtensions.ExtensionMethods
{
	public static class ContainerExtensions
	{
		public static bool Exists(this CloudBlobContainer container)
		{
			try
			{
				container.FetchAttributes();
				return true;
			}
			catch (StorageClientException e)
			{
				if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
				{
					return false;
				}
				throw;
			}
		}
	}
}
