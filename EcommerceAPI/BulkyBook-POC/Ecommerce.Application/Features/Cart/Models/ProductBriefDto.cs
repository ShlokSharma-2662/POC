namespace Ecommerce.Application.Features.Cart.Models
{
    public class ProductBriefDto
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string CategoryName { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}


