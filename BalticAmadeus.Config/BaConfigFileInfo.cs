using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BalticAmadeus.Config
{
    public class BaConfigFileInfo : IFileInfo
    {
        private CloudBlockBlob _blobBlock;

        public BaConfigFileInfo(CloudBlockBlob blobBlock)
        {
            _blobBlock = blobBlock;
        }

        public bool Exists => _blobBlock.ExistsAsync().Result;

        public long Length => 1;

        public string PhysicalPath => null;

        public string Name => _blobBlock.Name;

        public DateTimeOffset LastModified => default(DateTimeOffset);

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            var streamTask = _blobBlock.OpenReadAsync();
            return streamTask.Result;
        }
    }
}
