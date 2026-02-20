using MediatR;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.Features.Wishlist.Commands;
using Ecommerce.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Ecommerce.Application.Features.Wishlist.Models;

namespace Ecommerce.Application.Features.Wishlist.Handlers
{
    public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public AddToWishlistCommandHandler(AppDbContext context, ICacheService cacheService, ICacheInvalidationService cacheInvalidationService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _cacheInvalidationService = cacheInvalidationService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<bool> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if product exists
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken);

                if (product == null)
                    return false;

                // Check if already in wishlist (active or inactive)
                var existingWishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.ProductId == request.ProductId && 
                                             w.UserId == request.UserId, cancellationToken);

                if (existingWishlistItem != null)
                {
                    if (existingWishlistItem.IsActive)
                    {
                        return true; // Already active in wishlist
                    }
                    else
                    {
                        // Reactivate the existing item
                        existingWishlistItem.IsActive = true;
                        existingWishlistItem.AddedAt = DateTime.UtcNow; // Update the added date
                        await _context.SaveChangesAsync(cancellationToken);
                        
                        // Invalidate wishlist cache after successful addition
                        await _cacheInvalidationService.InvalidateWishlistCacheAsync(request.UserId);
                        
                        return true;
                    }
                }

                // Add new item to wishlist
                var wishlistItem = new Ecommerce.Domain.Entities.WishlistItem
                {
                    ProductId = request.ProductId,
                    UserId = request.UserId,
                    AddedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.WishlistItems.Add(wishlistItem);
                await _context.SaveChangesAsync(cancellationToken);

                // Invalidate wishlist cache after successful addition
                await _cacheInvalidationService.InvalidateWishlistCacheAsync(request.UserId);

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
