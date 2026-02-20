using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Metrics.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Metrics.Handlers
{
    public class GetSystemMetricsQueryHandler : IRequestHandler<GetSystemMetricsQuery, PagedResult<SystemMetric>>
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GetSystemMetricsQueryHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetSystemMetricsQueryHandler(AppDbContext context, ILogger<GetSystemMetricsQueryHandler> logger, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<PagedResult<SystemMetric>> Handle(GetSystemMetricsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔍 Processing GetSystemMetricsQuery with parameters: {@Request}", request);
            
            // Generate cache key based on request parameters
            var cacheKey = CacheKeyBuilder.SystemMetricsWithFilters(
                request.PageNumber, 
                request.PageSize, 
                request.EndpointFilter, 
                request.StatusCodeFilter, 
                request.FromDate, 
                request.ToDate);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<PagedResult<SystemMetric>>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("📈 Retrieved {Count} metrics from cache for page {Page}", cachedData.Items.Count, request.PageNumber);
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var query = _context.SystemMetrics.AsQueryable();

            // Apply endpoint filter
            if (!string.IsNullOrWhiteSpace(request.EndpointFilter))
            {
                query = query.Where(m => m.Endpoint.Contains(request.EndpointFilter));
            }

            // Apply status code filter
            if (request.StatusCodeFilter.HasValue)
            {
                query = query.Where(m => m.StatusCode == request.StatusCodeFilter.Value);
            }

            // Apply date range filter
            if (request.FromDate.HasValue)
            {
                query = query.Where(m => m.Timestamp >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(m => m.Timestamp <= request.ToDate.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            _logger.LogInformation("📊 Total metrics count: {TotalCount}", totalCount);

            var metrics = await query
                .OrderByDescending(m => m.Timestamp)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("📈 Retrieved {Count} metrics for page {Page}", metrics.Count, request.PageNumber);

            var result = new PagedResult<SystemMetric>
            {
                Items = metrics,
                TotalCount = totalCount
            };

            // Cache the result for future requests (shorter expiration for metrics)
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5)); // 5 minutes for metrics
            }

            return result;
        }
    }
}
