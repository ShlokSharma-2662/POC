using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.Features.Wishlist.Models
{
    public class RemoveFromWishlistRequest
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public long UserId { get; set; }
    }
}
