using Microsoft.Extensions.FileProviders;
using System;
using Microsoft.Extensions.Primitives;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BalticAmadeus.Config
{
    public class BaConfigFileProvider : IFileProvider
    {
        private CloudBlockBlob _blobBlock;
        public BaConfigFileProvider(CloudBlockBlob blobBlock)
        {
            _blobBlock = blobBlock;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return new BaConfigFileInfo(_blobBlock);
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }
    }
}
