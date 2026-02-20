namespace Ecommerce.Application.Features.Wishlist.Models
{
    public class WishlistItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public decimal ProductPrice { get; set; }
        public string ProductImageUrl { get; set; }
        public string CategoryName { get; set; }
        public int Stock { get; set; }
        public DateTime AddedAt { get; set; }
        public bool IsInStock => Stock > 0;
    }
}
