using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace ConfigurationStorageManager
{
    public class CloudStorageService
    {
        private CloudBlobClient _client;
        public bool IsConnected { get; }

        public CloudStorageService(string connectionString)
        {
            CloudStorageAccount.TryParse(connectionString, out var cloudAccount);
            if (cloudAccount != null)
            {
                _client = cloudAccount.CreateCloudBlobClient();
                IsConnected = true;
            }
            else
            {
                IsConnected = false;
            }
        }

        public async Task<ContainerResultSegment> GetContainersFromCloudAsync()
        {
            return await _client.ListContainersSegmentedAsync(null);
        }

        public async Task<BlobResultSegment> GetBlobsFromCloudAsync(CloudBlobContainer container)
        {
            return await container.ListBlobsSegmentedAsync(null);
        }

        public async Task<string> GetDataFromBlobAsync(CloudBlockBlob blob)
        {
            var stream = await blob.OpenReadAsync();
            var dataStream = new StreamReader(stream);
            var data = await dataStream.ReadToEndAsync();
            return data;
        }

        public async Task UploadDataToBlobAsync(CloudBlockBlob blob, string content)
        {
            await blob.UploadTextAsync(content);
        }

        public async Task<CloudBlockBlob> AddBlobAsync(CloudBlobContainer container, string blobName, string content)
        {
            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadTextAsync(content);
            return blob;
        }

        public async Task RemoveBlobAsync(CloudBlockBlob blob)
        {
            await blob.DeleteIfExistsAsync();
        }
    }
}
