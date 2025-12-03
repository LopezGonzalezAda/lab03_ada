using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MCT.Functions.Models; // Ensure this matches your namespace for UploadEntity

namespace MCT.Functions
{
    public class GetImages
    {
        private readonly ILogger<GetImages> _logger;

        public GetImages(ILogger<GetImages> logger)
        {
            _logger = logger;
        }

        [Function("GetImages")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("Processing GetImages request.");

            // 1. Get the email from the query string
            string email = req.Query["email"];

            if (string.IsNullOrEmpty(email))
            {
                return new BadRequestObjectResult("Please pass an email on the query string");
            }

            // 2. Connect to the Table
            string connectionString = System.Environment.GetEnvironmentVariable("StorageAccount");
            var tableClient = new TableClient(connectionString, "uploads");

            // 3. Query the table
            // We want all entities where PartitionKey (Email) matches the input
            var queryResults = tableClient.Query<UploadEntity>(filter: $"PartitionKey eq '{email}'");

            // 4. Map the results to a cleaner format (optional, but matches your lab screenshot)
            var resultList = new List<object>();

            foreach (var entity in queryResults)
            {
                resultList.Add(new
                {
                    description = entity.Description,
                    confidence = entity.Confidence,
                    image = entity.RowKey, // The filename is stored in RowKey
                    email = entity.PartitionKey
                });
            }

            // 5. Return the JSON
            return new OkObjectResult(resultList);
        }
    }
}