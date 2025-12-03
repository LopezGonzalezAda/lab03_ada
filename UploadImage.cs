using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace MCT.Functions;

public class UploadImage
{
    private readonly ILogger<UploadImage> _logger;

    private readonly BlobServiceClient _blobServiceClient;

    public UploadImage(ILogger<UploadImage> logger, BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    [Function("UploadImage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        // 1. Check for files
        if (req.Form.Files.Count == 0)
        {
            return new BadRequestObjectResult("No files were uploaded.");
        }

        var file = req.Form.Files[0];
        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        // --- NEW CODE STARTS HERE ---

        // 2. Get a reference to the container
        var containerClient = _blobServiceClient.GetBlobContainerClient("images");

        // 3. Ensure the container exists
        await containerClient.CreateIfNotExistsAsync();

        // 4. Get a reference to the blob
        var blobClient = containerClient.GetBlobClient(fileName);

        // 5. Upload the file
        await using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                }
            });
        }

        // --- NEW CODE ENDS HERE ---

        return new OkObjectResult("Image uploaded successfully!");
    }
}