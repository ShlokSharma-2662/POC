using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ecommerce.Application.Common.Services
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long? GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // Try common claim names: "sub", NameIdentifier, or custom "uid"
            var idValue =
                user.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("uid");

            if (string.IsNullOrWhiteSpace(idValue) || !long.TryParse(idValue, out var userId))
            {
                return null;
            }

            return userId;
        }

        public long GetCurrentUserIdOrThrow()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("User is not authenticated or user ID is missing/invalid");
            }

            return userId.Value;
        }

        public string? GetClaimValue(string claimType)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirstValue(claimType);
        }

        public bool IsAuthenticated()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true;
        }
    }
}
