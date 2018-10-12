using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveCardsReleasesHelper.Helpers
{
    public static class BlobHelper
    {
        public static Task SaveObjectAsync(string filename, object obj)
        {
            return SaveBlobAsync(filename, JsonConvert.SerializeObject(obj));
        }

        public static async Task SaveBlobAsync(string filename, string str)
        {
            // Parse connection string
            if (CloudStorageAccount.TryParse(Startup.BlobStorageConnectionString, out CloudStorageAccount account))
            {
                var client = account.CreateCloudBlobClient();

                var container = client.GetContainerReference("releaseshelper");
                await container.CreateIfNotExistsAsync();

                // Set permissions so blobs are public
                await container.SetPermissionsAsync(new Microsoft.WindowsAzure.Storage.Blob.BlobContainerPermissions()
                {
                    PublicAccess = Microsoft.WindowsAzure.Storage.Blob.BlobContainerPublicAccessType.Blob
                });

                // Upload or overwrite
                var blob = container.GetBlockBlobReference(filename);
                await blob.UploadTextAsync(str);
            }
            else
            {
                throw new Exception("Invalid blob storage connection string");
            }
        }

        public static async Task<T> GetObjectAsync<T>(string filename, int cacheDurationInMinutes = 5)
        {
            // Parse connection string
            if (CloudStorageAccount.TryParse(Startup.BlobStorageConnectionString, out CloudStorageAccount account))
            {
                var client = account.CreateCloudBlobClient();

                var container = client.GetContainerReference("releaseshelper");
                var blob = container.GetBlobReference(filename);

                await blob.FetchAttributesAsync();

                if (blob.Properties.LastModified.Value.AddMinutes(cacheDurationInMinutes) < DateTime.UtcNow)
                {
                    throw new Exception("Outdated blob");
                }

                using (var stream = await blob.OpenReadAsync())
                {
                    JsonSerializer serializer = new JsonSerializer();

                    using (var reader = new StreamReader(stream))
                    {
                        using (var jsonReader = new JsonTextReader(reader))
                        {
                            return serializer.Deserialize<T>(jsonReader);
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Invalid blob storage connection string");
            }
        }

        public static async Task<T> GetCachedOrRefresh<T>(string filename, Func<Task<T>> refreshFuncAsync, int cacheDurationInMinutes = 5)
        {
            try
            {
                if (cacheDurationInMinutes > 0)
                {
                    return await GetObjectAsync<T>(filename, cacheDurationInMinutes: cacheDurationInMinutes);
                }
            }
            catch { }

            T newObj = await refreshFuncAsync();

            await SaveObjectAsync(filename, newObj);

            return newObj;
        }
    }
}
