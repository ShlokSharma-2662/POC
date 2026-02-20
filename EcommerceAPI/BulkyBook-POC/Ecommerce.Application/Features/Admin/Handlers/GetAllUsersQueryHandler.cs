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
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, PagedResult<AdminUserDto>>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetAllUsersQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<PagedResult<AdminUserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            // Generate cache key based on request parameters
            var cacheKey = CacheKeyBuilder.AdminUsersWithFilters(
                request.PageNumber, 
                request.PageSize, 
                request.Status, 
                request.Role, 
                request.SearchTerm);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<PagedResult<AdminUserDto>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    (u.Username != null && u.Username.ToLower().Contains(search)) ||
                    (u.FirstName != null && u.FirstName.ToLower().Contains(search)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(search))
                );
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(u => u.Status == request.Status);
            }
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                query = query.Where(u => u.Role == request.Role);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = users.Select(u => new AdminUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            }).ToList();

            var result = new PagedResult<AdminUserDto>
            {
                Items = items,
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
}


