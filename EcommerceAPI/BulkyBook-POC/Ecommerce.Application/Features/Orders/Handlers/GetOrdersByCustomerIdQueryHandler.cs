using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Application.Features.Orders.Queries;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Orders.Handlers
{
    public class GetOrdersByCustomerIdQueryHandler : IRequestHandler<GetOrdersByCustomerIdQuery, List<OrderDto>>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetOrdersByCustomerIdQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<List<OrderDto>> Handle(GetOrdersByCustomerIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeyBuilder.UserOrders(request.CustomerId);

            if (_isCacheEnabled == "true")
            {
                var cached = await _cacheService.GetAsync<List<OrderDto>>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }
            }

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(item => item.UserId == request.CustomerId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(OrderMappings.ToOrderDtoExpression)
                .ToListAsync(cancellationToken);

            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, orders);
            }

            return orders;
        }
    }
}
