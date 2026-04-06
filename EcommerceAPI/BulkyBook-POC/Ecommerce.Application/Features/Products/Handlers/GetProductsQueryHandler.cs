using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Application.Features.Products.Queries;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductViewModel>>
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;
    private readonly string _isCacheEnabled;
    private readonly TimeSpan _productCacheDuration;

    public GetProductsQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
    {
        _context = context;
        _cacheService = cacheService;
        _configuration = configuration;
        _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        _productCacheDuration = TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:ProductReadExpirationMinutes", 5));
    }

    public async Task<PagedResult<ProductViewModel>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var normalizedCategoryName = request.CategoryName?.Trim().ToLowerInvariant();
        var normalizedSearchTerm = request.SearchTerm?.Trim().ToLowerInvariant();
        var skip = (request.PageNumber - 1) * request.PageSize;

        var cacheKey = CacheKeyBuilder.PublicProductsWithFilters(
            request.PageNumber,
            request.PageSize,
            request.CategoryId,
            request.SearchTerm,
            request.MinPrice,
            request.MaxPrice,
            request.CategoryName,
            request.InStockOnly);

        if (_isCacheEnabled == "true")
        {
            var cachedData = await _cacheService.GetAsync<PagedResult<ProductViewModel>>(cacheKey);
            if (cachedData != null)
            {
                return cachedData;
            }
        }

        var totalCount = CompiledProductQueries.CountProducts(
            _context,
            request.CategoryId > 0 ? request.CategoryId : null,
            normalizedCategoryName,
            request.MinPrice,
            request.MaxPrice,
            request.InStockOnly,
            normalizedSearchTerm);

        var asyncProducts = CompiledProductQueries.SearchProductsPage(
                _context,
                request.CategoryId > 0 ? request.CategoryId : null,
                normalizedCategoryName,
                request.MinPrice,
                request.MaxPrice,
                request.InStockOnly,
                normalizedSearchTerm,
                skip,
                request.PageSize);

        var products = new List<ProductViewModel>();
        await foreach (var p in asyncProducts.WithCancellation(cancellationToken))
        {
            products.Add(p);
        }

        var result = new PagedResult<ProductViewModel>
        {
            Items = products,
            TotalCount = totalCount
        };

        if (_isCacheEnabled == "true")
        {
            await _cacheService.SetAsync(cacheKey, result, _productCacheDuration);
        }

        return result;
    }
}
