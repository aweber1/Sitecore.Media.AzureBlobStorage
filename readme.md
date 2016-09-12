# Sitecore Media Azure Blob Storage provider
This module allows to store Sitecore media library assets in the Azure Blob Storage account.

# Instructions
* Restore nuget packages and build the solution.
* Move ```Sitecore.Media.AzureBlobStorage.dll``` to the ```\bin``` folder of your Sitecore application.
* Move ```App_Config\Include\Sitecore.Media.AzureBlobStorage``` folder to the ```App_Config\Include``` directory of you Sitecore application.
* Adjust settings in the ```App_Config\Include\Sitecore.Media.AzureBlobStorage\Sitecore.Media.AzureBlobStorage.config``` file if necessary.
* Disable ```Media.DisableFileMedia``` setting in your Sitecore application configuration.

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
The solution needs to be recompiled with a target version of ```Sitecore.Kernel.dll``` and thoroughly tested before you can upgrade to a new version of Sitecore platform.  
Since the module replaces data provider API which works with SQL Server blob field, 
the file based media should be completely disabled in your Sitecore solution to make sure all media assets use a single blob storage mechanism. 
> File based media option is not supported and must be disabled when this module is used.

# Dependencies
* [Windows Azure Storage](https://www.nuget.org/packages/WindowsAzure.Storage/) nuget package.
* Sitecore.Kernel

# License
This module is licensed under the [MIT License](LICENSE).

# Download
Donloads are available via [GitHub Releases](https://github.com/aweber1/Sitecore.Media.AzureBlobStorage/releases).
