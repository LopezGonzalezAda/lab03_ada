using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Azure;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddAzureClients(clientBuilder =>
{
    // This registers the BlobServiceClient using the connection string from local.settings.json
    // The connection string key must match what you have in local.settings.json (e.g. "StorageAccount")
    clientBuilder.AddBlobServiceClient(Environment.GetEnvironmentVariable("StorageAccount"));
});

builder.Build().Run();
