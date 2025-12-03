namespace MCT.Functions.Models
{
    public class FraudCheckResponse
    {
        public string OrderId { get; set; }
        public bool IsFraud { get; set; }
        public string Customer { get; set; }
        public string Country { get; set; }
        public string Reason { get; set; }
        public override string ToString()
        {
            return $"OrderId: {OrderId}, IsFraud: {IsFraud}, Customer: {Customer}, Country: {Country}, Reason: {Reason}";
        }
    }
}