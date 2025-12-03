using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MCT.Functions.Models;
using Azure.Data.Tables;

namespace MCT.Functions
{
    public class UploadOrder
    {
        private readonly ILogger<UploadOrder> _logger;

        public UploadOrder(ILogger<UploadOrder> logger)
        {
            _logger = logger;
        }

        [Function("UploadOrder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order")] HttpRequest req)
        {
            _logger.LogInformation("Receiving order...");

            var order = await req.ReadFromJsonAsync<Order>();
            if (order == null) return new BadRequestObjectResult("Invalid order.");

            // 1. Prepare Data
            order.RowKey = order.OrderId;
            order.PartitionKey = order.Country;
            order.Timestamp = DateTimeOffset.UtcNow;
            order.IsFraud = false; 
            order.Reason = "Pending Fraud Check"; // We mark it as pending

            // 2. Save to Table Storage (Fast)
            string connectionString = Environment.GetEnvironmentVariable("StorageAccount");
            var tableClient = new TableClient(connectionString, "orders");
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.UpsertEntityAsync(order);

            // 3. Put message on Queue (Fast)
            QueueRepository queueRepository = new QueueRepository();
            queueRepository.SendMessage(order);

            _logger.LogInformation($"Order {order.OrderId} queued for background processing.");
            
            // Return immediately - we don't wait for the Fraud API anymore!
            return new OkObjectResult(order);
        }
    }
}