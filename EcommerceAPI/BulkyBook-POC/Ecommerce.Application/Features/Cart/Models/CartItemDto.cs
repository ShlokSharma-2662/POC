using Ecommerce.Application.Features.Cart.Models;

namespace Ecommerce.Application.Features.Cart.Models
{
    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public ProductBriefDto Product { get; set; } = null!;
    }
}


