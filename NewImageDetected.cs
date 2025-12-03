using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.AI.Vision.ImageAnalysis;

namespace MCT.Functions;

public class NewImageDetected
{
    private readonly ILogger<NewImageDetected> _logger;

    public NewImageDetected(ILogger<NewImageDetected> logger)
    {
        _logger = logger;
    }

        [Function(nameof(NewImageDetected))]
        public async Task Run([BlobTrigger("images/{name}", Connection = "StorageAccount")] Stream stream, string name)
        {
            string tempPath = Path.GetTempFileName();

            try
            {
                using (var fileStream = File.Create(tempPath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Temp file path: {tempPath}");

            }
            finally
            {
                // Clean up the temp file
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
}