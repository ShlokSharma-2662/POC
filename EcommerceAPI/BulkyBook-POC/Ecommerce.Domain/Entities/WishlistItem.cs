using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Domain.Entities
{
    public class WishlistItem
    {
        public int Id { get; set; }
        
        [Required]
        public long UserId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ApplicationUser User { get; set; }
        public Product Product { get; set; }
    }
}
