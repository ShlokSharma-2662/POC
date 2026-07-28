using Ecommerce.Infrastructure.Services;
using FluentAssertions;

namespace Ecommerce.Tests.Infrastructure;

public class MicrosoftOAuthEndpointBuilderTests
{
    [Theory]
    [InlineData(
        "https://login.microsoftonline.com/tenant-id",
        "https://login.microsoftonline.com/tenant-id/oauth2/v2.0/authorize")]
    [InlineData(
        "https://login.microsoftonline.com/tenant-id/v2.0",
        "https://login.microsoftonline.com/tenant-id/oauth2/v2.0/authorize")]
    public void BuildV2Endpoint_AcceptsAuthorityWithOrWithoutVersionSuffix(
        string authority,
        string expectedEndpoint)
    {
        var result = MicrosoftOAuthEndpointBuilder.BuildV2Endpoint(authority, "authorize");

        result.Should().Be(expectedEndpoint);
    }
}
