using Ecommerce.Application.Common.Services;
using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Application.Features.Orders.Queries;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
                return new List<OrderDto>();
            }

            var cacheKey = CacheKeyBuilder.UserOrders(userId.Value);

            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<List<OrderDto>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId.Value)
                .OrderByDescending(order => order.CreatedAt)
                .Select(OrderMappings.ToOrderDtoExpression)
                .ToListAsync(ct);

            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, orders);
            }

            return orders;
        }
    }
}
