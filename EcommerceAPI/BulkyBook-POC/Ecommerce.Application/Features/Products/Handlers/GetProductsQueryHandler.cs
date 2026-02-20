using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Products.Queries;
using Ecommerce.Application.Features.Products.Models;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductViewModel>>
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;
    private readonly string _isCacheEnabled;

    public GetProductsQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
    {
        _context = context;
        _cacheService = cacheService;
        _configuration = configuration;
        _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
    }

    public async Task<PagedResult<ProductViewModel>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Generate cache key based on request parameters
        var cacheKey = CacheKeyBuilder.PublicProductsWithFilters(
            request.PageNumber, 
            request.PageSize, 
            request.CategoryId, 
            request.SearchTerm,
            request.MinPrice,
            request.MaxPrice,
            request.CategoryName);

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
        var query = _context.Products
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        // Apply category filter by ID
        if (request.CategoryId > 0)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId);
        }

        // Apply category filter by name
        if (!string.IsNullOrWhiteSpace(request.CategoryName))
        {
            var categorySearch = request.CategoryName.ToLower();
            query = query.Where(p => p.Category != null && p.Category.Name.ToLower().Contains(categorySearch));
        }

        // Apply price range filters
        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        }

        // Apply search filter - enhanced to include price and category
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            
            // Log search parameters for debugging
            System.Diagnostics.Debug.WriteLine($"Search term: '{search}'");
            
            // Simple and reliable search that should work
            query = query.Where(p => 
                p.Name.ToLower().Contains(search) || 
                p.Description.ToLower().Contains(search) ||
                (p.Category != null && p.Category.Name.ToLower().Contains(search)) ||
                // Price search: convert price to string and check if it contains the search term
                p.Price.ToString().Contains(search)
            );
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
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
            Items = products,
            TotalCount = totalCount
        };

        // Cache the result for future requests
        if (_isCacheEnabled == "true")
        {
            await _cacheService.SetAsync(cacheKey, result);
        }

        return result;
    }
}
