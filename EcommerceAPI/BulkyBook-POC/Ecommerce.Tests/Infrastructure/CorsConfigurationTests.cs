using Ecommerce.Infrastructure.DependencyInjection;
using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ecommerce.Tests.Infrastructure;

public class CorsConfigurationTests
{
    [Fact]
    public void AddCorsServices_UsesConfiguredOriginsAndKeepsLocalhost4200()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = "https://shop.example.com",
                ["Cors:AllowedOrigins:0"] = "https://admin.example.com"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddCorsServices(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = options.GetPolicy("AllowAll");

        policy.Should().NotBeNull();
        policy!.Origins.Should().Contain(
            "https://shop.example.com",
            "https://admin.example.com",
            "http://localhost:4200",
            "https://localhost:4200");
        policy.SupportsCredentials.Should().BeTrue();
        policy.ExposedHeaders.Should().Contain("X-RateLimit-Reset");
    }
}
