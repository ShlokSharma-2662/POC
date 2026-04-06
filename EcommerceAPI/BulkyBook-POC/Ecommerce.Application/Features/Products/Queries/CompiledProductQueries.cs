using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Products.Queries
{
    internal static class CompiledProductQueries
    {
        private static readonly Func<AppDbContext, int?, string?, decimal?, decimal?, bool?, string?, int> CountProductsQuery =
            EF.CompileQuery((AppDbContext context, int? categoryId, string? categoryName, decimal? minPrice, decimal? maxPrice, bool? inStockOnly, string? searchTerm) =>
                context.Products
                    .AsNoTracking()
                    .Where(product => !product.IsDeleted)
                    .Where(product => !categoryId.HasValue || product.CategoryId == categoryId.Value)
                    .Where(product => string.IsNullOrWhiteSpace(categoryName) ||
                                      (product.Category != null && product.Category.Name.ToLower().Contains(categoryName!)))
                    .Where(product => !minPrice.HasValue || product.Price >= minPrice.Value)
                    .Where(product => !maxPrice.HasValue || product.Price <= maxPrice.Value)
                    .Where(product => !inStockOnly.HasValue || !inStockOnly.Value || product.Stock > 0)
                    .Where(product => string.IsNullOrWhiteSpace(searchTerm) ||
                                      product.Name.ToLower().Contains(searchTerm!) ||
                                      product.Description.ToLower().Contains(searchTerm!) ||
                                      (product.Category != null && product.Category.Name.ToLower().Contains(searchTerm!)))
                    .Count());

        private static readonly Func<AppDbContext, int?, string?, decimal?, decimal?, bool?, string?, int, int, IAsyncEnumerable<ProductViewModel>> SearchProductsPageQuery =
            EF.CompileAsyncQuery((AppDbContext context, int? categoryId, string? categoryName, decimal? minPrice, decimal? maxPrice, bool? inStockOnly, string? searchTerm, int skip, int take) =>
                context.Products
                    .AsNoTracking()
                    .Where(product => !product.IsDeleted)
                    .Where(product => !categoryId.HasValue || product.CategoryId == categoryId.Value)
                    .Where(product => string.IsNullOrWhiteSpace(categoryName) ||
                                      (product.Category != null && product.Category.Name.ToLower().Contains(categoryName!)))
                    .Where(product => !minPrice.HasValue || product.Price >= minPrice.Value)
                    .Where(product => !maxPrice.HasValue || product.Price <= maxPrice.Value)
                    .Where(product => !inStockOnly.HasValue || !inStockOnly.Value || product.Stock > 0)
                    .Where(product => string.IsNullOrWhiteSpace(searchTerm) ||
                                      product.Name.ToLower().Contains(searchTerm!) ||
                                      product.Description.ToLower().Contains(searchTerm!) ||
                                      (product.Category != null && product.Category.Name.ToLower().Contains(searchTerm!)))
                    .OrderBy(product => product.Name)
                    .Skip(skip)
                    .Take(take)
                    .Select(ProductMappings.ToViewModelExpression));

        public static int CountProducts(
            AppDbContext context,
            int? categoryId,
            string? categoryName,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStockOnly,
            string? searchTerm)
        {
            return CountProductsQuery(context, categoryId, categoryName, minPrice, maxPrice, inStockOnly, searchTerm);
        }

        public static IAsyncEnumerable<ProductViewModel> SearchProductsPage(
            AppDbContext context,
            int? categoryId,
            string? categoryName,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStockOnly,
            string? searchTerm,
            int skip,
            int take)
        {
            return SearchProductsPageQuery(
                context,
                categoryId,
                categoryName,
                minPrice,
                maxPrice,
                inStockOnly,
                searchTerm,
                skip,
                take);
        }
    }
}
