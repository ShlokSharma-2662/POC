using Ecommerce.Application.Features.Categories.Queries;
using Ecommerce.Application.Features.Categories.Models;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Categories.Handlers
{
    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, List<Category>>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetAllCategoriesQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<List<Category>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            // Generate cache key
            var cacheKey = CacheKeyBuilder.AllCategories();

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<List<Category>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var data = await _context.Categories
                .Select(c => new
                {
                    Id = c.CategoryId,
                    Name = c.Name
                })
                .Distinct() // optional
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var result = data.Select(c => new Category
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            // Cache the result for future requests
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, result);
            }

            return result;
        }
    }
}
