namespace Ecommerce.Application.Features.Orders.Models
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = null!;
        public string ShippingAddress { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        
        public string Status { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }
    public static class OrderStatuses
    {
        public const string Pending = "Pending";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";

        public static readonly string[] All = { Pending, Shipped, Delivered };
    }
}
