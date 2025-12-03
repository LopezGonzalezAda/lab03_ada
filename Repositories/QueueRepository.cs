using Azure.Storage.Queues;
using System;
using System.Text.Json;
using MCT.Functions.Models;

namespace MCT.Functions
{
    public class QueueRepository
    {
        public void SendMessage(Order order)
        {
            string connectionString = Environment.GetEnvironmentVariable("StorageAccount");
            
            // Create the client and ensure the queue exists
            var queueClient = new QueueClient(connectionString, "orders");
            queueClient.CreateIfNotExists();

            // Convert Order to JSON string
            string message = JsonSerializer.Serialize(order);
            
            // Convert JSON string to Bytes, then to Base64 (Required for Azure Queues)
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            var base64Message = Convert.ToBase64String(bytes);

            // Send it!
            queueClient.SendMessage(base64Message);
        }
    }
}