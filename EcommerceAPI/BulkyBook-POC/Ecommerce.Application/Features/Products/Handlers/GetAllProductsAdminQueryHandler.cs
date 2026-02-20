using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products.Queries;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Products.Handlers
{
    public class GetAllProductsAdminQueryHandler : IRequestHandler<GetAllProductsAdminQuery, PagedResult<ProductViewModel>>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetAllProductsAdminQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<PagedResult<ProductViewModel>> Handle(GetAllProductsAdminQuery request, CancellationToken cancellationToken)
        {
            // Generate cache key based on request parameters
            var cacheKey = CacheKeyBuilder.AdminProductsWithFilters(
                request.PageNumber, 
                request.PageSize, 
                request.IsDeleted, 
                request.CategoryId, 
                request.Search);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<PagedResult<ProductViewModel>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var query = _context.Products.AsQueryable();

            if (request.IsDeleted.HasValue)
            {
                query = query.Where(p => p.IsDeleted == request.IsDeleted.Value);
            }

            if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                    IsDeleted = p.IsDeleted
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResult<ProductViewModel>
            {
                Items = items,
                TotalCount = total
            };

            // Cache the result for future requests
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, result);
            }

            return result;
        }
    }
}



