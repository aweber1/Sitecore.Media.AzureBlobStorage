# Sitecore Media Azure Blob Storage provider
This module allows to store Sitecore media library assets in the Azure Blob Storage account.

# Instructions
* Restore nuget packages and build the solution.
* Move ```Sitecore.Media.AzureBlobStorage.dll``` to the ```\bin``` folder of your Sitecore application.
* Move ```App_Config\Include\Sitecore.Media.AzureBlobStorage``` folder to the ```App_Config\Include``` directory of you Sitecore application.
* Adjust settings in the ```App_Config\Include\Sitecore.Media.AzureBlobStorage\Sitecore.Media.AzureBlobStorage.config``` file if necessary.
* Disable ```Media.DisableFileMedia``` setting in your Sitecore application configuration.
* Set ```maxRequestLength``` attribute of ```httpRuntime``` section to set the size limit in KB for large media assets that you want to upload into Sitecore media library.
* Set ```maxAllowedContentLength``` attribute of ```/system.webServer/security/requestFiltering/requestLimits``` section to match the value specified in ```maxRequestLength``` attribute. This value should be in bytes instead of KB.
* Set ```Media.MaxSizeInDatabase``` setting to match size limit specified in ```maxRequestLength``` attribute. This value could be specified in KB, MB or GB.  

> There are a few functional tests in the solution that require Azure Storage Emulator to be running. 
You can install it as a [standalone application](https://go.microsoft.com/fwlink/?linkid=717179&clcid=0x409) or a part of [Microsoft Azure SDK](https://azure.microsoft.com/downloads/) package.

# Implementation details
This module overrides the following methods from default implementation of SqlServerDataProvider in order to replace underlying blob storage for media assets:  
* ```GetBlobStream(Guid, CallContext)```
* ```BlobStreamExists(Guid, CallContext)```
* ```RemoveBlobStream(Guid, CallContext)```
* ```SetBlobStream(Stream, Guid, CallContext)```
* ```CleanupBlobs(CallContext)```

## Pros
The implementation substitutes low-level Data API which makes it possible to minimize amount of changes necessary to plug in alternative blob storage implementation.  
Media management actions like _Upload_, _Download_, _Attach_, _Detach_ work seamlessly with this approach and do not require any other changes. 

## Cons
Due to the fact that changes are made to SqlServerDataProvider level, there is not enough abstraction to seamlessly replace Sitecore version when needed. 
The solution needs to be recompiled with a target version of ```Sitecore.Kernel.dll``` and thoroughly tested before you can upgrade to a desired version of Sitecore platform.  
The module forces all media assets to be stored in Azure Blob Storage and does not allow alternative storage. 
Thus, the file based media should be completely disabled in your Sitecore solution. 
> File based media option is not supported and must be disabled when this module is used.

# Dependencies
* [Windows Azure Storage](https://www.nuget.org/packages/WindowsAzure.Storage/) nuget package.
* Sitecore.Kernel
* [Azure Blob Storage emulator](https://go.microsoft.com/fwlink/?linkid=717179&clcid=0x409) for functional tests.

# License
This module is licensed under the [MIT License](LICENSE).

# Download
Donloads are available via [GitHub Releases](https://github.com/aweber1/Sitecore.Media.AzureBlobStorage/releases).

# Alternative solution
If you find this approach too fragile for your liking, you may consider [@jammykam's solution](https://github.com/jammykam/Sitecore-CloudMediaLibrary/).