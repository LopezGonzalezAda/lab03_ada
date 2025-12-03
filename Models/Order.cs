using Azure;
using Azure.Data.Tables;

public class Order : ITableEntity
{
    public string OrderId { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }

    public string City { get; set; }

    public string Country { get; set; }

    public double AmountToPay { get; set; }


    public string PartitionKey { get; set; }

    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public bool IsFraud { get; set; }
    public string Reason { get;   set; }
}