using System.Security.Claims;

namespace Ecommerce.Application.Common.Services
{
    public interface IUserContextService
    {
        /// <summary>
        /// Gets the current user ID from JWT claims
        /// </summary>
        /// <returns>The user ID as long, or null if not found/invalid</returns>
        long? GetCurrentUserId();

        /// <summary>
        /// Gets the current user ID from JWT claims and throws an exception if not found
        /// </summary>
        /// <returns>The user ID as long</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated or user ID is missing/invalid</exception>
        long GetCurrentUserIdOrThrow();

        /// <summary>
        /// Gets a specific claim value from the current user
        /// </summary>
        /// <param name="claimType">The claim type to retrieve</param>
        /// <returns>The claim value or null if not found</returns>
        string? GetClaimValue(string claimType);

        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        /// <returns>True if user is authenticated, false otherwise</returns>
        bool IsAuthenticated();
    }
}
