namespace Ecommerce.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public Product? Product { get; set; } = null!; // ✅ Required
    }

}
