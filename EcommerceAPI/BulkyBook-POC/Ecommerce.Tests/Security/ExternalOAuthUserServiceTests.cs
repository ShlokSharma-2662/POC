using Ecommerce.API.Security;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ecommerce.Tests.Security;

public sealed class ExternalOAuthUserServiceTests
{
    [Fact]
    public async Task UpsertAsync_CreatesPersistentUserAndKeepsStableIdWhenEmailChanges()
    {
        await using var context = CreateContext();
        var service = new ExternalOAuthUserService(
            context,
            NullLogger<ExternalOAuthUserService>.Instance);

        var firstLogin = await service.UpsertAsync(
            "microsoft",
            UserInfo("external-subject", "first@example.com", "Ada", "Lovelace"));
        var secondLogin = await service.UpsertAsync(
            "microsoft",
            UserInfo("external-subject", "updated@example.com", "Ada", "Byron"));

        firstLogin.Id.Should().BeGreaterThan(0);
        secondLogin.Id.Should().Be(firstLogin.Id);
        secondLogin.Email.Should().Be("updated@example.com");
        secondLogin.LastName.Should().Be("Byron");
        secondLogin.Role.Should().Be("User");
        (await context.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpsertAsync_LinksByEmailAndNeverDowngradesExistingRole()
    {
        await using var context = CreateContext();
        var admin = new ApplicationUser
        {
            Username = "admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("existing-password"),
            FirstName = "Existing",
            LastName = "Admin",
            Role = "Admin",
            Status = "Active"
        };
        context.Users.Add(admin);
        await context.SaveChangesAsync();
        var service = new ExternalOAuthUserService(
            context,
            NullLogger<ExternalOAuthUserService>.Instance);

        var result = await service.UpsertAsync(
            "google",
            UserInfo("google-subject", "ADMIN@example.com", "Updated", "Name"));

        result.Id.Should().Be(admin.Id);
        result.Role.Should().Be("Admin");
        result.FirstName.Should().Be("Updated");
        (await context.Users.CountAsync()).Should().Be(1);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static OAuthUserInfo UserInfo(
        string subject,
        string email,
        string givenName,
        string familyName) =>
        new()
        {
            Subject = subject,
            Email = email,
            Name = $"{givenName} {familyName}",
            GivenName = givenName,
            FamilyName = familyName,
            EmailVerified = true
        };
}
