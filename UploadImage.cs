using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace MCT.Functions;

public class UploadImage
{
    private readonly ILogger<UploadImage> _logger;

    public UploadImage(ILogger<UploadImage> logger)
    {
        _logger = logger;
    }

    [Function("UploadImage")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        // Check if the request contains any files
        if (req.Form.Files.Count == 0)
        {
            return new BadRequestObjectResult("No files were uploaded.");
        }
        // Get the file
        var file = req.Form.Files[0];

        // Generate a unique file name
        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        // This is just to test it works for now (we will replace this return later)
        return new OkObjectResult($"Received file: {file.FileName}");
    }
}