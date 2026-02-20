using MediatR;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Application.Features.Wishlist.Queries;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Wishlist.Handlers
{
    public class CheckWishlistStatusQueryHandler : IRequestHandler<CheckWishlistStatusQuery, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public CheckWishlistStatusQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<bool> Handle(CheckWishlistStatusQuery request, CancellationToken cancellationToken)
        {
            // Generate cache key for wishlist status
            var cacheKey = CacheKeyBuilder.WishlistStatus(request.UserId, request.ProductId);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<string>(cacheKey);
                if (cachedData != null && bool.TryParse(cachedData, out var cachedResult))
                {
                    return cachedResult;
                }
            }

            // If not in cache, fetch from database
            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.ProductId == request.ProductId && 
                                         w.UserId == request.UserId && 
                                         w.IsActive, cancellationToken);

            var result = wishlistItem != null;

            // Cache the result for future requests (shorter expiration for status checks)
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, result.ToString(), TimeSpan.FromMinutes(5)); // 5 minutes for status
            }

            return result;
        }
    }
}
