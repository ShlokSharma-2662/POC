using Microsoft.AspNetCore.Http;

namespace Ecommerce.Application.Features.Products.Models
{
    public class CreateProductWithImageRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public IFormFile? Image { get; set; }
    }
}
