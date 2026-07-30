using Ecommerce.Application.Features.Products.Commands;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Products.Handlers
{
    public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly IProductStockUpdateNotifier _stockUpdateNotifier;

        public DeleteProductHandler(
            AppDbContext context,
            ICacheInvalidationService cacheInvalidationService,
            IProductStockUpdateNotifier stockUpdateNotifier)
        {
            _context = context;
            _cacheInvalidationService = cacheInvalidationService;
            _stockUpdateNotifier = stockUpdateNotifier;
        }

        public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken);
            if (product == null) return false;

            product.IsDeleted = !request.Restore;
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate product and category cache after successful delete/restore
            await _cacheInvalidationService.InvalidateProductCacheAsync(request.ProductId);
            await _cacheInvalidationService.InvalidateCategoryCacheAsync();
            await _stockUpdateNotifier.NotifyProductStockUpdatedAsync(
                product.ProductId,
                product.Name,
                request.Restore ? product.Stock : 0,
                request.Restore ? "Restored" : "Deleted");

            return true;
        }
    }
}



