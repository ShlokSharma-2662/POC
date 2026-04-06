using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Application.Features.Products.Queries;
using HotChocolate.Authorization;
using MediatR;

namespace Ecommerce.ProductService.GraphQL
{
    [Authorize]
    public class ProductQuery
    {
        public Task<PagedResult<ProductViewModel>> GetProductsAsync(
            [Service] IMediator mediator,
            int pageNumber = 1,
            int pageSize = 20,
            int? categoryId = null,
            string? searchTerm = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? categoryName = null,
            bool? inStockOnly = null,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new GetProductsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                CategoryId = categoryId,
                SearchTerm = searchTerm,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                CategoryName = categoryName,
                InStockOnly = inStockOnly
            }, cancellationToken);
        }

        public Task<ProductViewModel?> GetProductByIdAsync(
            int productId,
            [Service] IMediator mediator,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new GetProductByIdQuery { ProductId = productId }, cancellationToken);
        }
    }
}
