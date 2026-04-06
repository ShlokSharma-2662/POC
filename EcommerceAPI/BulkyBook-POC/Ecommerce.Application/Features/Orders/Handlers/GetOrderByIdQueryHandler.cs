using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Application.Features.Orders.Queries;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Orders.Handlers
{
    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetOrderByIdQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeyBuilder.OrderById(request.OrderId);

            if (_isCacheEnabled == "true")
            {
                var cached = await _cacheService.GetAsync<OrderDto>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }
            }

            var order = await _context.Orders
                .AsNoTracking()
                .Where(item => item.Id == request.OrderId)
                .Select(OrderMappings.ToOrderDtoExpression)
                .FirstOrDefaultAsync(cancellationToken);

            if (order != null && _isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, order);
            }

            return order;
        }
    }
}
