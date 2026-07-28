namespace Ecommerce.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = null!;
        public string ShippingAddress { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public long UserId { get; set; }
        public string? PaymentIntentId { get; set; }
        public string CartFingerprint { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "usd";
        public List<OrderItem> Items { get; set; } = new();
    }
}
