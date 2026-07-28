using Ecommerce.API.Controllers;
using Ecommerce.API.Security;
using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ecommerce.Tests.Controllers;

public class GoogleOAuthControllerTests
{
    [Fact]
    public void Login_WithExternalReturnUrl_ReturnsValidationError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleOAuth:ClientId"] = "client-id",
                ["Frontend:BaseUrl"] = "https://shop.example.com"
            })
            .Build();
        var controller = new GoogleOAuthController(
            Mock.Of<IGoogleOAuthService>(),
            configuration,
            Mock.Of<ILogger<GoogleOAuthController>>(),
            Mock.Of<IJwtTokenGenerator>(),
            Mock.Of<IExternalOAuthUserService>(),
            Mock.Of<IOAuthAuthorizationCodeStore>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Login("https://attacker.example/collect");

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ApiResponse>();
    }

    [Fact]
    public async Task Logout_ReturnsStandardApiResponse()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(x => x.SignOutAsync(
                It.IsAny<HttpContext>(),
                "Cookie",
                It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        var requestServices = new ServiceCollection()
            .AddSingleton(authenticationService.Object)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestServices
        };
        var controller = new GoogleOAuthController(
            Mock.Of<IGoogleOAuthService>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<GoogleOAuthController>>(),
            Mock.Of<IJwtTokenGenerator>(),
            Mock.Of<IExternalOAuthUserService>(),
            Mock.Of<IOAuthAuthorizationCodeStore>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.Logout();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value
            .Should().BeOfType<ApiResponse<OAuthLogoutResponse>>().Subject;
        response.IsSuccessful.Should().BeTrue();
        response.Data!.LogoutUrl.Should().Be("https://accounts.google.com/logout");
    }

    [Fact]
    public async Task Exchange_ConsumesGoogleBoundGrant()
    {
        var store = new Mock<IOAuthAuthorizationCodeStore>();
        store.Setup(x => x.ConsumeAsync(
                "google",
                "opaque-code",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OAuthAuthorizationGrant(
                "jwt-token",
                7,
                "Grace",
                "Hopper",
                "grace@example.com",
                "User",
                null,
                "/my-orders",
                DateTimeOffset.UtcNow.AddMinutes(1)));
        var controller = new GoogleOAuthController(
            Mock.Of<IGoogleOAuthService>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<GoogleOAuthController>>(),
            Mock.Of<IJwtTokenGenerator>(),
            Mock.Of<IExternalOAuthUserService>(),
            store.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Exchange(
            new OAuthCodeExchangeRequest("opaque-code"),
            CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value
            .Should().BeOfType<ApiResponse<OAuthCodeExchangeResponse>>().Subject;
        envelope.Data!.Token.Should().Be("jwt-token");
        envelope.Data.ReturnTo.Should().Be("/my-orders");
        store.VerifyAll();
    }
}
