using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApp6
{
    public class AzureStorage
    {
        public readonly IConfiguration _config;

        private string _ConnectionString;
        private string ConnectionString
        {
            get
            {
                return _ConnectionString;
            }
        }

        public AzureStorage(IConfiguration config)
        {
            _config = config;
            _ConnectionString = _config.GetConnectionString("MyAzureStorageAccount");
        }

        public async Task<List<BlobInfo>> GetContainerBlobs(string containerName)
        {
            var blobs = new List<BlobInfo>();
            var container = new BlobContainerClient(ConnectionString, containerName);
            var blobItems = container.GetBlobsAsync();
            await foreach (var blobItem in blobItems)
            {
                var blob = container.GetBlobClient(blobItem.Name);
                var properties = await blob.GetPropertiesAsync();
                blobs.Add(new() { 
                    Url = blob.Uri.ToString(),
                    Name = blob.Name, 
                    ContentType = properties.Value.ContentType, 
                    LastModified = properties.Value.LastModified.DateTime 
                });
            }
            return blobs;
        }

        public async Task<bool> AddNewContainer(string containerName)
        {
            var container = new BlobContainerClient(ConnectionString, containerName);
            var created = await container.CreateIfNotExistsAsync();
            if (created != null && created.GetRawResponse().Status == 201)
            {
                await container.SetAccessPolicyAsync(PublicAccessType.Blob);
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteContainer(string containerName)
        {
            var container = new BlobContainerClient(ConnectionString, containerName);
            var deleted = await container.DeleteIfExistsAsync();
            return deleted != null && deleted.GetRawResponse().Status == 410;
        }

        public async Task<string> UploadBlob(string containerName, IBrowserFile file)
        {
            var container = new BlobContainerClient(ConnectionString, containerName);
            var blob = container.GetBlobClient(file.Name);
            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
            using (var stream = file.OpenReadStream())
            {
                await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }
            return blob.Uri.ToString();
        }

        public async void DeleteBlob(string containerName, string filename)
        {
            var container = new BlobContainerClient(ConnectionString, containerName);
            var blob = container.GetBlobClient(filename);
            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }
}
