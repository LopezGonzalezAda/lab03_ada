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

            // --- CALL THE FUNCTION HERE ---
            AnalyzeImage(tempPath); 
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    // --- PASTE THE HELPER METHOD HERE (Inside the class, but outside Run) ---
    public void AnalyzeImage(string path)
    {
        // FIX THE TYPO HERE: "AIEndpoint" instead of "AIEndppoint"
        string endpoint = Environment.GetEnvironmentVariable("AIEndpoint");
        string key = Environment.GetEnvironmentVariable("AIKey");

        ImageAnalysisClient client = new ImageAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(key));
        
        BinaryData imageData = BinaryData.FromBytes(File.ReadAllBytes(path));

        ImageAnalysisResult result = client.Analyze(
            imageData,
            VisualFeatures.Caption | VisualFeatures.Read,
            new ImageAnalysisOptions { GenderNeutralCaption = true });

        // Use _logger if you can, or Console.WriteLine if that's what the lab asks
        System.Console.WriteLine("Image analysis results:");
        System.Console.WriteLine(" Caption:");
        System.Console.WriteLine($"   '{result.Caption.Text}', Confidence {result.Caption.Confidence:F4}");
    }
}