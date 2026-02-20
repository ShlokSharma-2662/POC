using Ecommerce.Application.Features.Products.Commands;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Products.Handlers
{
    public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheInvalidationService _cacheInvalidationService;

        public UpdateProductHandler(AppDbContext context, ICacheInvalidationService cacheInvalidationService)
        {
            _context = context;
            _cacheInvalidationService = cacheInvalidationService;
        }

        public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken);
            if (product == null) return false;

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.ImageUrl = request.ImageUrl;
            product.Stock = request.Stock;
            product.CategoryId = request.CategoryId;
            if (string.IsNullOrWhiteSpace(product.ImageUrl) && !string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                product.ImageUrl = request.ImageUrl;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate product and category cache after successful update
            await _cacheInvalidationService.InvalidateProductCacheAsync(request.ProductId);
            await _cacheInvalidationService.InvalidateCategoryCacheAsync();

            return true;
        }
    }
}



