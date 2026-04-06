using Ecommerce.Application.Features.Products;
using Ecommerce.Application.Features.Products.Commands;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Infrastructure.Persistence;
using HotChocolate.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.ProductService.GraphQL
{
    public class ProductMutation
    {
        [Authorize(Policy = "AdminOnly")]
        public async Task<ProductViewModel?> CreateProductAsync(
            CreateProductCommand input,
            [Service] IMediator mediator,
            [Service] AppDbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            var productId = await mediator.Send(input, cancellationToken);

            return await dbContext.Products
                .AsNoTracking()
                .Where(product => product.ProductId == productId && !product.IsDeleted)
                .Select(ProductMappings.ToViewModelExpression)
                .FirstOrDefaultAsync(cancellationToken);
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<ProductViewModel?> UpdateProductAsync(
            int productId,
            UpdateProductCommand input,
            [Service] IMediator mediator,
            [Service] AppDbContext dbContext,
            CancellationToken cancellationToken = default)
        {
            input.ProductId = productId;

            var updated = await mediator.Send(input, cancellationToken);
            if (!updated)
            {
                return null;
            }

            return await dbContext.Products
                .AsNoTracking()
                .Where(product => product.ProductId == productId)
                .Select(ProductMappings.ToViewModelExpression)
                .FirstOrDefaultAsync(cancellationToken);
        }

        [Authorize(Policy = "AdminOnly")]
        public Task<bool> DeleteProductAsync(
            int productId,
            [Service] IMediator mediator,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteProductCommand { ProductId = productId }, cancellationToken);
        }

        [Authorize(Policy = "AdminOnly")]
        public Task<bool> RestoreProductAsync(
            int productId,
            [Service] IMediator mediator,
            CancellationToken cancellationToken = default)
        {
            return mediator.Send(new DeleteProductCommand { ProductId = productId, Restore = true }, cancellationToken);
        }
    }
}
