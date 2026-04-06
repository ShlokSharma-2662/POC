using System.Linq.Expressions;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Features.Products
{
    public static class ProductMappings
    {
        public static readonly Expression<Func<Product, ProductViewModel>> ToViewModelExpression = product => new ProductViewModel
        {
            ProductId = product.ProductId,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            CategoryName = product.Category != null ? product.Category.Name : string.Empty,
            Stock = product.Stock,
            StockQuantity = product.Stock,
            StockStatus = product.Stock <= 0 ? "OutOfStock" : product.Stock < 10 ? "LowStock" : "InStock",
            IsDeleted = product.IsDeleted
        };

        public static ProductViewModel ToViewModel(this Product product)
        {
            return new ProductViewModel
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryName = product.Category?.Name ?? string.Empty,
                Stock = product.Stock,
                StockQuantity = product.Stock,
                StockStatus = product.Stock <= 0 ? "OutOfStock" : product.Stock < 10 ? "LowStock" : "InStock",
                IsDeleted = product.IsDeleted
            };
        }
    }
}
