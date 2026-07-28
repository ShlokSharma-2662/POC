using Ecommerce.API.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Tests.Security;

public class OAuthRedirectUrlHelperTests
{
    [Fact]
    public void TryResolveFrontendReturnUrl_WithRelativePath_UsesConfiguredFrontendOrigin()
    {
        var configuration = BuildConfiguration(
            ("Frontend:BaseUrl", "https://shop.example.com"));

        var result = OAuthRedirectUrlHelper.TryResolveFrontendReturnUrl(
            configuration,
            "/orders?filter=open",
            out var resolvedUrl);

        result.Should().BeTrue();
        resolvedUrl.Should().Be("https://shop.example.com/orders?filter=open");
    }

    [Fact]
    public void TryResolveFrontendReturnUrl_WithConfiguredAbsoluteOrigin_IsAllowed()
    {
        var configuration = BuildConfiguration(
            ("Frontend:BaseUrl", "https://shop.example.com"),
            ("Frontend:AllowedOrigins:0", "https://preview.example.com"));

        var result = OAuthRedirectUrlHelper.TryResolveFrontendReturnUrl(
            configuration,
            "https://preview.example.com/oauth/callback?source=login",
            out var resolvedUrl);

        result.Should().BeTrue();
        resolvedUrl.Should().Be("https://preview.example.com/oauth/callback?source=login");
    }

    [Theory]
    [InlineData("https://attacker.example/collect")]
    [InlineData("//attacker.example/collect")]
    [InlineData("/\\attacker.example/collect")]
    [InlineData("javascript:alert(1)")]
    public void TryResolveFrontendReturnUrl_WithUntrustedTarget_IsRejected(string returnUrl)
    {
        var configuration = BuildConfiguration(
            ("Frontend:BaseUrl", "https://shop.example.com"));

        var result = OAuthRedirectUrlHelper.TryResolveFrontendReturnUrl(
            configuration,
            returnUrl,
            out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void BuildCallbackUri_UsesConfiguredPublicOriginInsteadOfRequestHost()
    {
        var configuration = BuildConfiguration(
            ("OAuth:BaseUrl", "https://api.example.com"),
            ("OAuth:CallbackPath", "/api/oauth/callback"));
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("internal-container", 8080);

        var callbackUri = OAuthRedirectUrlHelper.BuildCallbackUri(
            context.Request,
            configuration,
            "OAuth",
            "/api/oauth/callback");

        callbackUri.Should().Be("https://api.example.com/api/oauth/callback");
    }

    [Fact]
    public void AppendAuthorizationCode_PreservesQueryAndPlacesOpaqueCodeBeforeFragment()
    {
        var redirectUrl = OAuthRedirectUrlHelper.AppendAuthorizationCode(
            "https://shop.example.com/oauth/callback?source=google#complete",
            "code+/=",
            "Google");

        redirectUrl.Should().Be(
            "https://shop.example.com/oauth/callback?source=google&code=code%2B%2F%3D&provider=google#complete");
        redirectUrl.Should().NotContain("jwt_token");
    }

    [Theory]
    [InlineData("/checkout?step=payment")]
    [InlineData("/admin")]
    public void TryResolveFrontendReturnPath_WithLocalPath_IsAllowed(string returnTo)
    {
        OAuthRedirectUrlHelper.TryResolveFrontendReturnPath(returnTo, out var resolved)
            .Should().BeTrue();
        resolved.Should().Be(returnTo);
    }

    [Theory]
    [InlineData("https://attacker.example")]
    [InlineData("//attacker.example")]
    [InlineData("/\\attacker.example")]
    public void TryResolveFrontendReturnPath_WithUnsafePath_IsRejected(string returnTo)
    {
        OAuthRedirectUrlHelper.TryResolveFrontendReturnPath(returnTo, out _)
            .Should().BeFalse();
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                pair => pair.Key,
                pair => (string?)pair.Value))
            .Build();
    }
}
