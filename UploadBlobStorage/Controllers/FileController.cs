using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;

using System.IO;
using System.Threading.Tasks;
using UploadBlobStorage.ViewModel;

namespace UploadBlobStorage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private Container _container;
        // ADD THIS PART TO YOUR CODE
        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = "https://devopsapresentacao.documents.azure.com:443/";
        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = "LTkL9GjDS5ZCIX8abujMNNrmEmKWlHhEWIRZWeKrJ3xSHZ02NmWtzSQiEtKg7PrIm4dFmMAQkOgIegQU8HGgvA==";
        // The Cosmos client instance
        private CosmosClient cosmosClient;
        // The database we will create
        private Database database;
        // The name of the database and container we will create
        private string databaseId = "ImagensDatabase";
        private string containerId = "ImagensContainer";
        private readonly IConfiguration _configuration;
        public FileController(IConfiguration configuration
             )
        {
            _configuration = configuration;
        }

        [HttpPost(nameof(UploadFile))]
        public async Task<IActionResult> UploadFile(IList<IFormFile> files)
        {
            foreach (var file in files)
            {
                var _guid = Guid.NewGuid().ToString().Substring(0, 8);
                string systemFileName = $"artigo_{_guid}.jpg";
                string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
                // Retrieve storage account from connection string.    
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
                // Create the blob client.    
                CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
                // Retrieve a reference to a container.    
                CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetValue<string>("BlobContainerName"));
                // This also does not make a service call; it only creates a local object.    
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(systemFileName);
                await using (var data = file.OpenReadStream())
                {
                    await blockBlob.UploadFromStreamAsync(data);
                }
               
                try
                {
                    var item = new ImagemViewModel()
                    {
                        Id = Guid.NewGuid().ToString(),
                        PathOriginal = $"https://blobdevopsapresentacao.blob.core.windows.net/imagens/{systemFileName}",
                        PathThumbs = $"https://blobdevopsapresentacao.blob.core.windows.net/thumbs/{systemFileName}",
                        PathResize = $"https://blobdevopsapresentacao.blob.core.windows.net/imagens-resize/{systemFileName}",
                        Date = DateTime.Now,
                        ImagemId = Guid.NewGuid(),
                    };
                    await AddItemAsync(item);
                }
                catch (Exception ex)
                {

                    return BadRequest("Erro ao salvar no cosmos : " + ex.Message);
                }
               
            }

            return Ok("Files Uploaded Successfully");
        }

        [NonAction]
        public async Task AddItemAsync(ImagemViewModel item)
        {
            try
            {

                Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient("https://devopsapresentacao.documents.azure.com:443/", "LTkL9GjDS5ZCIX8abujMNNrmEmKWlHhEWIRZWeKrJ3xSHZ02NmWtzSQiEtKg7PrIm4dFmMAQkOgIegQU8HGgvA==");
                this._container = client.GetContainer(databaseId, containerId);
                await this._container.CreateItemAsync<ImagemViewModel>(item, new PartitionKey(item.Id));

            }
            catch (Exception)
            {

                throw;
            }

        }


        [HttpPost(nameof(DownloadFile))]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            CloudBlockBlob blockBlob;
            await using (MemoryStream memoryStream = new MemoryStream())
            {
                string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(_configuration.GetValue<string>("BlobContainerName"));
                blockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                await blockBlob.DownloadToStreamAsync(memoryStream);
            }
            Stream blobStream = blockBlob.OpenReadAsync().Result;
            return File(blobStream, blockBlob.Properties.ContentType, blockBlob.Name);
        }

        [HttpDelete(nameof(DeleteFile))]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            string strContainerName = _configuration.GetValue<string>("BlobContainerName");
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);
            var blob = cloudBlobContainer.GetBlobReference(fileName);
            await blob.DeleteIfExistsAsync();
            return Ok("File Deleted");
        }

        //[HttpGet("teste")]
        //public async Task<IActionResult> Teste()
        //{
        //    try
        //    {
        //        var systemFileName = "nome1.jpg";
        //        var item = new ImagemViewModel()
        //        {
        //            Id = Guid.NewGuid().ToString(),
        //            PathOriginal = $"https://blobdevopsapresentacao.blob.core.windows.net/imagens/{systemFileName}",
        //            PathThumbs = $"https://blobdevopsapresentacao.blob.core.windows.net/thumbs/{systemFileName}",
        //            PathResize = $"https://blobdevopsapresentacao.blob.core.windows.net/imagens-resize/{systemFileName}",
        //            Date = DateTime.Now,
        //            ImagemId = Guid.NewGuid(),
        //        };
        //        await AddItemAsync(item);
        //    }
        //    catch (Exception ex)
        //    {

        //        return BadRequest("Erro ao salvar no cosmos : " + ex.Message);
        //    }

        //    return Ok("chegou no teste");
        //}
    }
}
