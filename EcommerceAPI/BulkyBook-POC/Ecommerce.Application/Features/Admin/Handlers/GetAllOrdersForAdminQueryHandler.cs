using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Admin.Models;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class
        GetAllOrdersForAdminQueryHandler : IRequestHandler<GetAllOrdersForAdminQuery, PagedResult<AdminOrderDto>>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetAllOrdersForAdminQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<PagedResult<AdminOrderDto>> Handle(GetAllOrdersForAdminQuery request,
            CancellationToken cancellationToken)
        {
            // Generate cache key based on request parameters
            var cacheKey = CacheKeyBuilder.AdminOrdersWithFilters(
                request.PageNumber, 
                request.PageSize, 
                request.Status, 
                request.SearchTerm);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<PagedResult<AdminOrderDto>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var query = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(o =>
                        o.CustomerName.ToLower().Contains(search) ||
                        o.Id.ToString().ToLower().Contains(search) ||
                        o.Phone.ToLower().Contains(search)
                );
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(o => o.Status == request.Status);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var result = orders.Select(order => new AdminOrderDto
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                Phone = order.Phone,
                ShippingAddress = order.ShippingAddress,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                Items = order.Items.Select(i => new AdminOrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Unknown",
                    Description = i.Product?.Description ?? "",
                    Price = i.Product?.Price ?? 0,
                    Quantity = i.Quantity
                }).ToList()
            }).ToList();

            var pagedResult = new PagedResult<AdminOrderDto>
            {
                Items = result,
                TotalCount = totalCount
            };

            // Cache the result for future requests
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, pagedResult);
            }

            return pagedResult;
        }
    }
}