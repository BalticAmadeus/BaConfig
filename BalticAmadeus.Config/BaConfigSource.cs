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
        private bool _required;

        public BaConfigSource(string blobName, bool required)
        {
            _blobName = blobName;
            _required = required;
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
                Optional = !_required;
                if (!Optional)
                {
                    var blobBlock = GetBlobBlockReferenceFromCloud(environmentString);
                    FileProvider = new BaConfigFileProvider(blobBlock);
                }
            }
            return new JsonConfigurationProvider(this);
        }

        public CloudBlockBlob GetBlobBlockReferenceFromCloud(string environmentString)
        {
            if (!environmentString.StartsWith("ContainerName"))
                throw new Exception("BACONFIG_TARGET format is invalid. Valid format: ContainerName=....;ConnectionString");

            var endIndex = environmentString.IndexOf(';');
            var beginIndex = environmentString.IndexOf('=');

            var containerName = environmentString.Substring(beginIndex + 1, endIndex - beginIndex - 1);
            var connectionString = environmentString.Substring(endIndex + 1);

            var blobContainer = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient().GetContainerReference(containerName);

            if (!blobContainer.ExistsAsync().Result)
                throw new Exception("BACONFIG_TARGET container does not exists.");

            Path = $"{containerName}/{_blobName}";
            return blobContainer.GetBlockBlobReference(_blobName);
        }
    }
}
