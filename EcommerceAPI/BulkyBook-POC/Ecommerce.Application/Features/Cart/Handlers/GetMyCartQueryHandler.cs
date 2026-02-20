using Ecommerce.Application.Features.Cart.Models;
using Ecommerce.Application.Features.Cart.Queries;
using Ecommerce.Application.Common.Services;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CartEntity = Ecommerce.Domain.Entities.Cart;

namespace Ecommerce.Application.Features.Cart.Handlers
{
    public class GetMyCartQueryHandler : IRequestHandler<GetMyCartQuery, List<CartItemDto>>
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userContextService;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetMyCartQueryHandler(AppDbContext context, IUserContextService userContextService, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _userContextService = userContextService;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<List<CartItemDto>> Handle(GetMyCartQuery request, CancellationToken cancellationToken)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return new List<CartItemDto>();
            }

            // Generate cache key for user-specific cart
            var cacheKey = CacheKeyBuilder.UserCart(userId.Value);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<List<CartItemDto>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value.ToString(), cancellationToken);

            if (cart == null)
            {
                cart = new CartEntity { UserId = userId.Value.ToString() };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var result = cart.Items.Select(i =>
            {
                var product = i.Product;
                return new CartItemDto
            {
                CartItemId = i.CartItemId,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Product = new ProductBriefDto
                {
                    ProductId = product?.ProductId ?? i.ProductId,
                    CategoryId = product?.CategoryId ?? 0,
                    Name = product?.Name ?? string.Empty,
                    Description = product?.Description ?? string.Empty,
                    Price = product?.Price ?? 0,
                    ImageUrl = product?.ImageUrl ?? string.Empty,
                    CategoryName = product?.Category?.Name ?? string.Empty,
                    Stock = product?.Stock ?? 0
                }
            }; }).ToList();

            // Cache the result for future requests
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, result);
            }

            return result;
        }
    }
}


