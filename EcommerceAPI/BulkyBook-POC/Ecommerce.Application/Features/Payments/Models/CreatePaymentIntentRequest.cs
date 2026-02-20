namespace Ecommerce.Application.Features.Payments.Models
{
    public class CreatePaymentIntentRequest
    {
        public long Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public string? CustomerEmail { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
