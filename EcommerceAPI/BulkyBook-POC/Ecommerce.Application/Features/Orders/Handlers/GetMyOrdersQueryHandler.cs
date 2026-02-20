using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Application.Features.Orders.Queries;
using Ecommerce.Application.Common.Services;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderItemDto = Ecommerce.Application.Features.Orders.Models.OrderItemDto;

namespace Ecommerce.Application.Features.Orders.Handlers
{
    public class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, List<OrderDto>>
    {
        private readonly AppDbContext _context;
        private readonly IUserContextService _userContextService;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetMyOrdersQueryHandler(AppDbContext context, IUserContextService userContextService, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _userContextService = userContextService;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<List<OrderDto>> Handle(GetMyOrdersQuery request, CancellationToken ct)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                // optionally: throw new UnauthorizedAccessException();
                return new(); // or return empty list
            }

            // Generate cache key for user-specific orders
            var cacheKey = CacheKeyBuilder.UserOrders(userId.Value);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<List<OrderDto>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId.Value)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    CustomerName = o.CustomerName,
                    ShippingAddress = o.ShippingAddress,
                    Phone = o.Phone,
                    CreatedAt = o.CreatedAt,
                    Status = string.IsNullOrEmpty(o.Status) ? "Pending" : o.Status,
                    Items = o.Items.Select(i => new OrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product != null ? i.Product.Name : "Product not found",
                        Price = i.Product != null ? i.Product.Price : 0,
                        Quantity = i.Quantity,
                        ImageUrl = i.Product != null ? i.Product.ImageUrl : ""
                    }).ToList()
                })
                .ToListAsync(ct);

            // Cache the result for future requests
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, orders);
            }

            return orders;
        }
    }

}
