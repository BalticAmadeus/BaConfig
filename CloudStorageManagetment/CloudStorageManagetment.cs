using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace ConfigurationStorageManager
{
    public static class CloudStorageManagetment
    {
        private static CloudBlobClient _client;

        public static bool CreateConnectionWithCloud(string connectionString)
        {
            CloudStorageAccount.TryParse(connectionString, out var cloudAccount);
            if (cloudAccount != null)
            {
                _client = cloudAccount.CreateCloudBlobClient();
                return true;
            }
            else
            {
                return false;
            }
        }

        public static  async Task<ContainerResultSegment> GetContainersFromCloudAsync()
        {
            return await _client.ListContainersSegmentedAsync(null);
        }

        public static async Task<BlobResultSegment> GetBlobsFromCloudAsync(CloudBlobContainer container)
        {
            return await container.ListBlobsSegmentedAsync(null);
        }

        public static async Task<string> GetDataFromBlobAsync(CloudBlockBlob blob)
        {
            var stream = await blob.OpenReadAsync();
            var dataStream = new StreamReader(stream);
            var data = await dataStream.ReadToEndAsync();
            return data;
        }

        public static async Task UploadDataToBlobAsync(CloudBlockBlob blob, string content)
        {
            await blob.UploadTextAsync(content);
        }

        public static async Task<CloudBlockBlob> AddNewBlobAsync(CloudBlobContainer container, string blobName, string content)
        {
            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadTextAsync(content);

            if (blobName.EndsWith(".json"))
            {
                blob.Properties.ContentType = "application/json";
                await blob.SetPropertiesAsync();
            }
            else if (blobName.EndsWith(".txt"))
            { 
                blob.Properties.ContentType = "text/plain";
                await blob.SetPropertiesAsync();
            }
            return blob;
        }

        public static async Task RemoveBlobAsync(CloudBlockBlob blob)
        {
            await blob.DeleteIfExistsAsync();
        }
    }
}
