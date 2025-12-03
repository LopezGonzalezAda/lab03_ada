using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Data.Tables;           // Needed for Table Storage
using Azure.Storage.Blobs;         // Needed to read metadata
using MCT.Functions.Models;        // Needed to see your UploadEntity class

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
            // 1. Save stream to temp file
            using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            _logger.LogInformation($"Processing image: {name}");

            // 2. GET EMAIL (New Step)
            string email = GetEmailFromBlobMetaData(name);

            // 3. ANALYZE & SAVE (Updated Step)
            // We pass the email and name so we can save them to the database
            AnalyzeImage(tempPath, name, email); 
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    // --- HELPER 1: ANALYZE IMAGE ---
    public void AnalyzeImage(string path, string fileName, string email)
    {
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

        // Log the result
        string caption = result.Caption.Text;
        double confidence = result.Caption.Confidence;
        _logger.LogInformation($"Analyzed: '{caption}', Confidence: {confidence:F4}");

        // 4. SAVE TO DATABASE (New Call)
        UpdateTableStorage(email, fileName, caption, confidence);
    }

    // --- HELPER 2: UPDATE TABLE STORAGE ---
    public void UpdateTableStorage(string email, string fileName, string description, double confidence)
    {
        try
        {
            string connectionString = Environment.GetEnvironmentVariable("StorageAccount");

            // Create TableClient
            var tableClient = new TableClient(connectionString, "uploads");

            // Ensure table exists
            tableClient.CreateIfNotExists();

            // Create the entity (using the class you created in Models)
            var uploadEntity = new UploadEntity(email, fileName)
            {
                Description = description,
                Confidence = confidence,
                UploadDate = DateTime.UtcNow,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Save it!
            tableClient.UpsertEntity(uploadEntity);
            _logger.LogInformation($"Saved to Table Storage: {fileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save to table: {ex.Message}");
        }
    }

    // --- HELPER 3: GET METADATA ---
    private string GetEmailFromBlobMetaData(string blobName)
    {
        string connectionString = Environment.GetEnvironmentVariable("StorageAccount");
        BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("images");
        BlobClient blobClient = containerClient.GetBlobClient(blobName);

        var properties = blobClient.GetProperties();
        
        // Check if metadata exists to prevent crashing
        if (properties.Value.Metadata.ContainsKey("email"))
        {
            return properties.Value.Metadata["email"];
        }
        return "unknown@example.com";
    }
}