using Ecommerce.API.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ecommerce.Tests.Security;

public sealed class OAuthAuthorizationCodeStoreTests
{
    [Fact]
    public async Task ConsumeAsync_IsAtomicAndSingleUse()
    {
        using var services = new ServiceCollection()
            .AddDistributedMemoryCache()
            .BuildServiceProvider();
        var store = CreateStore(services);
        var code = await store.IssueAsync("microsoft", CreateGrant());

        code.Should().MatchRegex("^[A-Za-z0-9_-]{43}$");
        code.Should().NotContain("jwt-token");

        var results = await Task.WhenAll(
            store.ConsumeAsync("microsoft", code),
            store.ConsumeAsync("microsoft", code));

        results.Count(result => result is not null).Should().Be(1);
        results.Single(result => result is not null)!.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task ConsumeAsync_BindsCodeToProvider()
    {
        using var services = new ServiceCollection()
            .AddDistributedMemoryCache()
            .BuildServiceProvider();
        var store = CreateStore(services);
        var code = await store.IssueAsync("google", CreateGrant());

        (await store.ConsumeAsync("microsoft", code)).Should().BeNull();
        (await store.ConsumeAsync("google", code)).Should().NotBeNull();
    }

    [Fact]
    public async Task ConsumeAsync_RejectsExpiredGrant()
    {
        using var services = new ServiceCollection()
            .AddDistributedMemoryCache()
            .BuildServiceProvider();
        var timeProvider = new MutableTimeProvider(
            new DateTimeOffset(2026, 7, 28, 0, 0, 0, TimeSpan.Zero));
        var store = CreateStore(services, timeProvider);
        var code = await store.IssueAsync("google", CreateGrant());
        timeProvider.Advance(TimeSpan.FromSeconds(31));

        (await store.ConsumeAsync("google", code)).Should().BeNull();
    }

    private static OAuthAuthorizationCodeStore CreateStore(
        IServiceProvider services,
        TimeProvider? timeProvider = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OAuth:AuthorizationCodeLifetimeSeconds"] = "30"
            })
            .Build();

        return new OAuthAuthorizationCodeStore(
            services.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(),
            configuration,
            services,
            NullLogger<OAuthAuthorizationCodeStore>.Instance,
            timeProvider);
    }

    private static OAuthAuthorizationGrant CreateGrant() =>
        new(
            "jwt-token",
            42,
            "Ada",
            "Lovelace",
            "ada@example.com",
            "User",
            null,
            "/checkout",
            default);

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public MutableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
