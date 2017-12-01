using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ConfigurationStorageManager
{
    public class LocalStorageService
    {
        public async Task SaveContainerInSelectedFolder(StorageFolder folder, CloudStorageService client, CloudBlobContainer container)
        {
            var containerBlobSegments = await client.GetBlobsFromCloudAsync(container);
            var blobsToSave = containerBlobSegments.Results.ToList().Cast<CloudBlockBlob>().ToList().Where(x => !x.Name.Contains(".secrets.")).ToList();

            var filesInFolder = await folder.GetFilesAsync();

            foreach (var blob in blobsToSave)
            {
                    SaveContentInFile(await folder.CreateFileAsync(blob.Name, CreationCollisionOption.ReplaceExisting),
                        await client.GetDataFromBlobAsync(blob));
            }
        }

        public async void SaveContentInFile(StorageFile file, string content)
        {
            using (var streamWriter = new StreamWriter(await file.OpenStreamForWriteAsync()))
            {
                streamWriter.BaseStream.SetLength(0);
                streamWriter.Write(content);
            }
        }
    }
}
