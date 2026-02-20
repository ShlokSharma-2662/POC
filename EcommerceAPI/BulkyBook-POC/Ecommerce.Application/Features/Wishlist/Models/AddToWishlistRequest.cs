using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.Features.Wishlist.Models
{
    public class AddToWishlistRequest
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public long UserId { get; set; }
    }
}
