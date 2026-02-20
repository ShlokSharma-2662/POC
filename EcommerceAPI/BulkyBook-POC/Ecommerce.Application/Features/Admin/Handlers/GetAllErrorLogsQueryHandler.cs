using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class GetAllErrorLogsQueryHandler : IRequestHandler<GetAllErrorLogsQuery, PagedResult<ErrorLog>>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetAllErrorLogsQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<PagedResult<ErrorLog>> Handle(GetAllErrorLogsQuery request, CancellationToken cancellationToken)
        {
            // Generate cache key based on request parameters
            var cacheKey = CacheKeyBuilder.ErrorLogsWithFilters(
                request.PageNumber, 
                request.PageSize, 
                request.SeverityFilter, 
                request.SearchTerm);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<PagedResult<ErrorLog>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var query = _context.ErrorLogs.AsQueryable();

            // Apply severity filter
            if (!string.IsNullOrWhiteSpace(request.SeverityFilter))
            {
                query = query.Where(a => a.Severity == request.SeverityFilter);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(a => 
                    (a.Message != null && a.Message.ToLower().Contains(search)) ||
                    (a.Path != null && a.Path.ToLower().Contains(search)) ||
                    (a.UserAgent != null && a.UserAgent.ToLower().Contains(search)) ||
                    (a.IpAddress != null && a.IpAddress.ToLower().Contains(search))
                );
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var errorLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<ErrorLog>
            {
                Items = errorLogs,
                TotalCount = totalCount
            };

            // Cache the result for future requests (shorter expiration for error logs)
            if (_isCacheEnabled == "true")
            {
                await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10)); // 10 minutes for error logs
            }

            return result;
        }
    }
}
