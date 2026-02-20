using Ecommerce.Application.Features.Admin.Models;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, AdminUserDto?>
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IConfiguration _configuration;
        private readonly string _isCacheEnabled;

        public GetUserByIdQueryHandler(AppDbContext context, ICacheService cacheService, IConfiguration configuration)
        {
            _context = context;
            _cacheService = cacheService;
            _configuration = configuration;
            _isCacheEnabled = _configuration["Redis:IsCacheEnabled"] ?? "false";
        }

        public async Task<AdminUserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            // Generate cache key for user by ID
            var cacheKey = CacheKeyBuilder.UserById(request.UserId);

            // Try to get data from cache first
            if (_isCacheEnabled == "true")
            {
                var cachedData = await _cacheService.GetAsync<AdminUserDto>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }
            }

            // If not in cache, fetch from database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user == null) return null;

            var result = new AdminUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
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


