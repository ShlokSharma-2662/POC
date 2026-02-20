using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Features.Categories.Models
{
    public class Category
    {
        public int Id { get; set; }              // Primary Key
        public string Name { get; set; } = null!; // Category name

        // Navigation property (optional)
        public ICollection<Product>? Products { get; set; }
    }
}
