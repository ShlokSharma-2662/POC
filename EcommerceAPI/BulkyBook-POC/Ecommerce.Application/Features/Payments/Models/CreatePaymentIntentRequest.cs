namespace Ecommerce.Application.Features.Payments.Models
{
    public class CreatePaymentIntentRequest
    {
        public List<CreatePaymentIntentItemRequest> Items { get; set; } = new();
        public string? CheckoutReference { get; set; }
    }

    public class CreatePaymentIntentItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
