using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Ecommerce.API.Security;

public interface IExternalOAuthUserService
{
    Task<ApplicationUser> UpsertAsync(
        string provider,
        OAuthUserInfo userInfo,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Maps an external identity to a persisted application user so JWT subject claims
/// always use the database-generated application user ID.
/// </summary>
public sealed class ExternalOAuthUserService : IExternalOAuthUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExternalOAuthUserService> _logger;

    public ExternalOAuthUserService(
        AppDbContext context,
        ILogger<ExternalOAuthUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApplicationUser> UpsertAsync(
        string provider,
        OAuthUserInfo userInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(userInfo.Subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(userInfo.Email);

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedEmail = userInfo.Email.Trim().ToLowerInvariant();
        var externalUsername = $"oauth:{normalizedProvider}:{userInfo.Subject.Trim()}";

        var user = await _context.Users.FirstOrDefaultAsync(
            candidate => candidate.Username == externalUsername,
            cancellationToken);

        user ??= await _context.Users.FirstOrDefaultAsync(
            candidate => candidate.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Username = externalUsername,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
                FirstName = userInfo.GivenName?.Trim() ?? string.Empty,
                LastName = userInfo.FamilyName?.Trim() ?? string.Empty,
                Role = "User",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Created application user {UserId} for {Provider} OAuth",
                user.Id,
                normalizedProvider);
            return user;
        }

        var changed = false;
        if (!string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal))
        {
            user.Email = normalizedEmail;
            changed = true;
        }

        var givenName = userInfo.GivenName?.Trim();
        if (!string.IsNullOrEmpty(givenName) &&
            !string.Equals(user.FirstName, givenName, StringComparison.Ordinal))
        {
            user.FirstName = givenName;
            changed = true;
        }

        var familyName = userInfo.FamilyName?.Trim();
        if (!string.IsNullOrEmpty(familyName) &&
            !string.Equals(user.LastName, familyName, StringComparison.Ordinal))
        {
            user.LastName = familyName;
            changed = true;
        }

        if (changed)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return user;
    }
}
