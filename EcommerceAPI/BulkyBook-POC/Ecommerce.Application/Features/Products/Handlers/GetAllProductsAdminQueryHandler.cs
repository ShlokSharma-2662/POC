using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Application.Features.Products.Queries;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
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
            var cacheKey = CacheKeyBuilder.AdminProductsWithFilters(
                request.PageNumber,
                request.PageSize,
                request.IsDeleted,
                request.CategoryId,
                request.Search);

            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<PagedResult<ProductViewModel>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            var query = _context.Products.AsNoTracking().AsQueryable();

            if (request.IsDeleted.HasValue)
            {
                query = query.Where(product => product.IsDeleted == request.IsDeleted.Value);
            }

            if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
            {
                query = query.Where(product => product.CategoryId == request.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.ToLowerInvariant();
                query = query.Where(product =>
                    product.Name.ToLower().Contains(term) ||
                    product.Description.ToLower().Contains(term));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(product => product.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ProductMappings.ToViewModelExpression)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<ProductViewModel>
            {
                Items = items,
                TotalCount = total
            };

            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, result);
            }

            return result;
        }
    }
}
