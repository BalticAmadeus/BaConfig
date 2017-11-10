using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;

namespace BalticAmadeus.Config
{
    public class BaConfigSource : JsonConfigurationSource
    {
        private string _blobName;

        public BaConfigSource(string blobName)
        {
            _blobName = blobName;
        }

        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var environmentString = Environment.GetEnvironmentVariable("BACONFIG_TARGET");
            if (environmentString == null)
            {
                Optional = true;
            }
            else
            {
                var blobBlock = GetBlobBlockFromCloud(environmentString);
                Optional = !blobBlock.ExistsAsync().Result;
                FileProvider = new BaConfigFileProvider(blobBlock);
            }
            return new JsonConfigurationProvider(this);
        }

        public CloudBlockBlob GetBlobBlockFromCloud(string environmentString)
        {
            if (!environmentString.StartsWith("ContainerName"))
                throw new Exception("BACONFIG_TARGET format is invalid. Valid format: ContainerName=....;ConnectionString");

            var endIndex = environmentString.IndexOf(';');
            var beginIndex = environmentString.IndexOf('=');

            var containerName = environmentString.Substring(beginIndex + 1, endIndex - beginIndex - 1);
            var connectionString = environmentString.Substring(endIndex + 1);

            var blobContainer = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient().GetContainerReference(containerName);

            if(!blobContainer.ExistsAsync().Result)
                throw new Exception("BACONFIG_TARGET container does not exists.");

            return blobContainer.GetBlockBlobReference(_blobName);
        }
    }
}
