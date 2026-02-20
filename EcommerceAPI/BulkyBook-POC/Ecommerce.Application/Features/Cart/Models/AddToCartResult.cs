namespace Ecommerce.Application.Features.Cart.Models
{
    public class AddToCartResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? MaxAddableQuantity { get; set; }
        public int? CurrentStock { get; set; }
        public int? RequestedQuantity { get; set; }
        public int? CurrentCartQuantity { get; set; }
    }
}

