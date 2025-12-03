using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Queues.Models;
using MCT.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace MCT.Functions
{
    public class CheckOrderFraud
    {
        private readonly ILogger<CheckOrderFraud> _logger;
        private readonly HttpClient _httpClient;

        public CheckOrderFraud(ILogger<CheckOrderFraud> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        [Function(nameof(CheckOrderFraud))]
        public async Task Run([QueueTrigger("orders", Connection = "StorageAccount")] QueueMessage message)
        {
            _logger.LogInformation($"Processing queue message: {message.MessageText}");

            // 1. Convert the queue message back into an Order object
            var order = JsonSerializer.Deserialize<Order>(message.MessageText);

            // 2. Call the Fraud API
            string fraudUrl = "https://cloud-service-fraud-api-aba8dwhxfwema9bu.canadacentral-01.azurewebsites.net/check/fraud";
            var jsonContent = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(fraudUrl, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                var fraudResult = await response.Content.ReadFromJsonAsync<FraudCheckResponse>();

                // 3. Update Table Storage with the final result
                string connectionString = Environment.GetEnvironmentVariable("StorageAccount");
                var tableClient = new TableClient(connectionString, "orders");

                // Retrieve the existing entity first (Best Practice to avoid overwriting other fields)
                var existingOrder = await tableClient.GetEntityAsync<Order>(order.Country, order.OrderId);
                
                existingOrder.Value.IsFraud = fraudResult.IsFraud;
                existingOrder.Value.Reason = fraudResult.Reason;

                await tableClient.UpsertEntityAsync(existingOrder.Value);
                
                _logger.LogInformation($"Fraud Check Complete. IsFraud: {fraudResult.IsFraud}");
            }
            else
            {
                _logger.LogError($"Error calling Fraud API: {response.ReasonPhrase}");
            }
        }
    }
}