using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MCT.Functions.Models;
using Azure.Data.Tables;
using System.Text;
using System.Net.Http.Json;

namespace MCT.Functions
{
    public class UploadOrder
    {
        private readonly ILogger<UploadOrder> _logger;
        private readonly HttpClient _httpClient; // Used to call the external API

        // We inject HttpClient via the constructor (Best Practice)
        public UploadOrder(ILogger<UploadOrder> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        [Function("UploadOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order")] HttpRequest req)
        {
            _logger.LogInformation("Processing new order...");

            // 1. Read the JSON body
            var order = await req.ReadFromJsonAsync<Order>();
            
            if (order == null) return new BadRequestObjectResult("Invalid order data.");

            // 2. Set Keys (Important for Table Storage!)
            // We use the OrderId as the RowKey so we can find it later
            order.RowKey = order.OrderId;
            order.PartitionKey = order.Country; // Group orders by country
            order.Timestamp = DateTimeOffset.UtcNow;

            // 3. Save Initial Order to Table Storage
            string connectionString = Environment.GetEnvironmentVariable("StorageAccount");
            var tableClient = new TableClient(connectionString, "orders"); // New table called 'orders'
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.UpsertEntityAsync(order);
            _logger.LogInformation($"Order {order.OrderId} saved to database.");

            // 4. Call the Fraud API
            string fraudUrl = "https://cloud-service-fraud-api-aba8dwhxfwema9bu.canadacentral-01.azurewebsites.net/check/fraud";
            
            // Convert the order to JSON to send it
            var jsonContent = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(fraudUrl, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                return new StatusCodeResult((int)response.StatusCode);
            }

            // 5. Read the Result
            var fraudResult = await response.Content.ReadFromJsonAsync<FraudCheckResponse>();

            // 6. Update the Order with the Result
            order.IsFraud = fraudResult.IsFraud;
            order.Reason = fraudResult.Reason;

            // 7. Save the Updated Order back to the Table
            await tableClient.UpsertEntityAsync(order);
            _logger.LogInformation($"Order updated. Fraud status: {order.IsFraud}");

            return new OkObjectResult(order);
        }
    }
}