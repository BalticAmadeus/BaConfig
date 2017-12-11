using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;

namespace BalticAmadeus.Config
{
    public class BaConfigSource : JsonConfigurationSource
    {
        private readonly string _blobName;
        private string _connectionString;

        public BaConfigSource(string connectionString,string blobName, bool required)
        {
            _blobName = blobName;
            Optional = !required;
            _connectionString = connectionString;
        }

        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            string connectionString = _connectionString ?? Environment.GetEnvironmentVariable("BACONFIG_TARGET");

            if (connectionString == null)
            {
                Optional = true;
                return new JsonConfigurationProvider(this);
            }
            else
            {
                return GetJsonConfigurationProvider(connectionString);
            }
        }

        private CloudBlockBlob GetBlobBlockReferenceFromCloud(string environmentString)
        {
            if (!environmentString.StartsWith("ContainerName"))
                throw new Exception("BACONFIG_TARGET format is invalid. Valid format: ContainerName=....;ConnectionString");

            var endIndex = environmentString.IndexOf(';');
            var beginIndex = environmentString.IndexOf('=');

            var containerName = environmentString.Substring(beginIndex + 1, endIndex - beginIndex - 1);
            var connectionString = environmentString.Substring(endIndex + 1);

            if (connectionString.StartsWith("LocalFileOverride="))
            {
                endIndex = connectionString.IndexOf(';');
                connectionString = connectionString.Substring(endIndex + 1);
            }

            var blobContainer = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient().GetContainerReference(containerName);

            if (!blobContainer.ExistsAsync().Result)
                throw new Exception("BACONFIG_TARGET container does not exists.");

            this.Path = $"{containerName}/{_blobName}";

            return blobContainer.GetBlockBlobReference(_blobName);
        }

        private JsonConfigurationProvider GetJsonConfigurationProvider(string connectionString)
        {
            var blobBlock = GetBlobBlockReferenceFromCloud(connectionString);
            FileProvider = new BaConfigFileProvider(blobBlock);
            return new JsonConfigurationProvider(this);
        }
    }
}
