using MediatR;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Application.Features.Wishlist.Models;
using Ecommerce.Application.Features.Wishlist.Queries;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Wishlist.Handlers
{
    public class GetUserWishlistQueryHandler : IRequestHandler<GetUserWishlistQuery, List<WishlistItemDto>>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetUserWishlistQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<List<WishlistItemDto>> Handle(GetUserWishlistQuery request, CancellationToken cancellationToken)
        {
            // Generate cache key for user-specific wishlist
            var cacheKey = CacheKeyBuilder.UserWishlist(request.UserId);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<List<WishlistItemDto>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var wishlistItems = await _context.WishlistItems
                .Where(w => w.UserId == request.UserId && w.IsActive)
                .Include(w => w.Product)
                .ThenInclude(p => p.Category)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => new WishlistItemDto
                {
                    Id = w.Id,
                    ProductId = w.ProductId,
                    ProductName = w.Product.Name,
                    ProductDescription = w.Product.Description,
                    ProductPrice = w.Product.Price,
                    ProductImageUrl = w.Product.ImageUrl,
                    CategoryName = w.Product.Category.Name,
                    Stock = w.Product.Stock,
                    AddedAt = w.AddedAt
                })
                .ToListAsync(cancellationToken);

            // Cache the result for future requests
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, wishlistItems);
            }

            return wishlistItems;
        }
    }
}
