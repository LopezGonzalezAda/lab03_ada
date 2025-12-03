using Azure;
using Azure.Data.Tables;
using System;

namespace MCT.Functions.Models // Changed namespace to match folder structure
{
    public class UploadEntity : ITableEntity
    {
        // Constructor that sets keys automatically
        public UploadEntity(string email, string fileName)
        {
            PartitionKey = email;
            RowKey = fileName;
        }

        // Empty constructor required by the library
        public UploadEntity() { }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        
        public double Confidence { get; set; }
        public string Description { get; set; }
        
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public DateTime UploadDate { get; set; }
    }
}