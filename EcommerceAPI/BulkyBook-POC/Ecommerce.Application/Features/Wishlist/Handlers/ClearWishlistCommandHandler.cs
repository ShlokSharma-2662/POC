using MediatR;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.Features.Wishlist.Commands;
using Ecommerce.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Ecommerce.Application.Features.Wishlist.Models;

namespace Ecommerce.Application.Features.Wishlist.Handlers
{
    public class ClearWishlistCommandHandler : IRequestHandler<ClearWishlistCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public ClearWishlistCommandHandler(AppDbContext context, ICacheService cacheService, ICacheInvalidationService cacheInvalidationService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _cacheInvalidationService = cacheInvalidationService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<bool> Handle(ClearWishlistCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var wishlistItems = await _context.WishlistItems
                    .Where(w => w.UserId == request.UserId && w.IsActive)
                    .ToListAsync(cancellationToken);

                if (!wishlistItems.Any())
                    return true; // Already empty

                foreach (var item in wishlistItems)
                {
                    item.IsActive = false;
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Invalidate wishlist cache after successful clearing
                // This must complete before returning to ensure cache is cleared
                if (_isCacheEnabled == "true")
                {
                    try
                    {
                        // Use the dedicated cache invalidation service
                        await _cacheInvalidationService.InvalidateWishlistCacheAsync(request.UserId);
                        
                        // Set an empty list in cache to ensure next request returns empty
                        var cacheKey = CacheKeyBuilder.UserWishlist(request.UserId);
                        await _cacheService.SetAsync(cacheKey, new List<WishlistItemDto>(), TimeSpan.FromMinutes(5));
                    }
                    catch (Exception)
                    {
                        // Log cache invalidation error but don't fail the operation
                        // Cache invalidation failure should not affect the main operation
                    }
                }

                return true;
            }
            catch (Exception)
            {
                // Log the error but don't throw to avoid breaking the application
                // Wishlist clearing failure should be handled gracefully
                return false;
            }
        }
    }
}
