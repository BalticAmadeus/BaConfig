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
    public class LocalStorage
    {
        public static async Task SaveContainerInSelectedFolder(StorageFolder folder, CloudStorageService client, CloudBlobContainer container)
        {
            var containerBlobSegments = await client.GetBlobsFromCloudAsync(container);
            var blobsToSave = containerBlobSegments.Results.ToList().Cast<CloudBlockBlob>().ToList().Where(x => !x.Name.Contains(".secrets.")).ToList();

            var filesInFolder = await folder.GetFilesAsync();

            foreach (var blob in blobsToSave)
            {
                var file = filesInFolder.SingleOrDefault(x => x.Name.Equals(blob.Name));
                if (file == null)
                    SaveContentInFile(await folder.CreateFileAsync(blob.Name), await client.GetDataFromBlobAsync(blob));
                else
                    SaveContentInFile(file, await client.GetDataFromBlobAsync(blob), true);
            }
        }

        public static void SaveContentInFile(StorageFile file ,string content)
        {
            SaveContentInFile(file, content, false);
        }

        public static async void SaveContentInFile(StorageFile file, string content, bool existingFile)
        {
            if(existingFile)
            {
                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteTextAsync(file, content);
                await CachedFileManager.CompleteUpdatesAsync(file);
            }
            else
            {
                using (var streamWriter = new StreamWriter(await file.OpenStreamForWriteAsync()))
                {
                    streamWriter.Write(content);
                }
            }
        }
    }
}
