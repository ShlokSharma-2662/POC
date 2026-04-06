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
    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductViewModel?>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;
        private readonly TimeSpan _productCacheDuration;

        public GetProductByIdQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
            _productCacheDuration = TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:ProductReadExpirationMinutes", 5));
        }

        public async Task<ProductViewModel?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeyBuilder.ProductById(request.ProductId);

            if (_isCacheEnabled == "true")
            {
                var cached = await _cacheService.GetAsync<ProductViewModel>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }
            }

            var product = await _context.Products
                .AsNoTracking()
                .Where(item => item.ProductId == request.ProductId && !item.IsDeleted)
                .Select(ProductMappings.ToViewModelExpression)
                .FirstOrDefaultAsync(cancellationToken);

            if (product != null && _isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, product, _productCacheDuration);
            }

            return product;
        }
    }
}
