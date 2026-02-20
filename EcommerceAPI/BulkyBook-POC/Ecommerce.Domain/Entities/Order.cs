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
        public List<OrderItem> Items { get; set; } = new();
    }
}