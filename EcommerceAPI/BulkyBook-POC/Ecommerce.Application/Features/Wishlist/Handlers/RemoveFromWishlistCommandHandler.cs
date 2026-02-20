using MediatR;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Application.Features.Wishlist.Commands;
using Ecommerce.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Ecommerce.Application.Features.Wishlist.Models;

namespace Ecommerce.Application.Features.Wishlist.Handlers
{
    public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public RemoveFromWishlistCommandHandler(AppDbContext context, ICacheService cacheService, ICacheInvalidationService cacheInvalidationService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _cacheInvalidationService = cacheInvalidationService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<bool> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.ProductId == request.ProductId && 
                                             w.UserId == request.UserId && 
                                             w.IsActive, cancellationToken);

                if (wishlistItem == null)
                    return false;

                // Soft delete by setting IsActive to false
                wishlistItem.IsActive = false;
                await _context.SaveChangesAsync(cancellationToken);

                // Invalidate wishlist cache after successful removal
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
