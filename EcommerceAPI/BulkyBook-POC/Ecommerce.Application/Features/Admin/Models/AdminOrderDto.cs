namespace Ecommerce.Application.Features.Admin.Models
{
    public class AdminOrderDto
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string ShippingAddress { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public List<AdminOrderItemDto> Items { get; set; } = new();
    }

    public class AdminOrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}