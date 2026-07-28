using Ecommerce.API.Controllers;
using Ecommerce.API.Security;
using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace Ecommerce.Tests.Controllers
{
    public class OAuthControllerTests : ControllerTestBase
    {
        private readonly Mock<IOAuthService> _mockOAuthService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<OAuthController>> _mockLogger;
        private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
        private readonly Mock<IExternalOAuthUserService> _mockExternalUserService;
        private readonly Mock<IOAuthAuthorizationCodeStore> _mockAuthorizationCodeStore;
        private readonly OAuthController _controller;

        public OAuthControllerTests()
        {
            _mockOAuthService = new Mock<IOAuthService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<OAuthController>>();
            _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
            _mockExternalUserService = new Mock<IExternalOAuthUserService>();
            _mockAuthorizationCodeStore = new Mock<IOAuthAuthorizationCodeStore>();
            _controller = new OAuthController(
                _mockOAuthService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockJwtTokenGenerator.Object,
                _mockExternalUserService.Object,
                _mockAuthorizationCodeStore.Object);
            SetupHttpContext();
            MockResponse.Setup(x => x.Headers).Returns(new HeaderDictionary());
        }

        private void AssertSuccessResponse<T>(ActionResult<T> result, int expectedStatusCode = 200)
        {
            result.Should().BeAssignableTo<ActionResult<T>>();
            var actionResult = result as ActionResult<T>;
            actionResult.Should().NotBeNull();

            if (actionResult!.Value != null)
            {
                actionResult.Value.Should().NotBeNull();
            }
            else if (actionResult.Result != null)
            {
                actionResult.Result.Should().BeOfType<OkObjectResult>();
                var okResult = actionResult.Result as OkObjectResult;
                okResult!.StatusCode.Should().Be(expectedStatusCode);
            }
            else
            {
                Assert.Fail("ActionResult should have either Value or Result set");
            }
        }

        [Fact]
        public void Login_WithValidReturnUrl_ReturnsRedirectResult()
        {
            // Arrange
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            _mockConfiguration.Setup(x => x["OAuth:Authority"]).Returns("https://login.microsoftonline.com/tenant-id");
            _mockConfiguration.Setup(x => x["OAuth:ClientId"]).Returns("client-id");
            _mockConfiguration.Setup(x => x["OAuth:Scope"]).Returns("openid profile email");
            _mockConfiguration.Setup(x => x["OAuth:CallbackPath"]).Returns("/signin-oidc");
            _mockConfiguration.Setup(x => x["OAuth:ResponseType"]).Returns("code");

            var returnUrl = "/dashboard";

            // Act
            var result = _controller.Login(returnUrl);

            // Assert
            result.Should().BeOfType<RedirectResult>();
            var redirectResult = result as RedirectResult;
            redirectResult.Should().NotBeNull();
            redirectResult!.Url.Should().Contain("login.microsoftonline.com");
            GetSessionString("oauth_return_url").Should().Be("http://localhost:4200/dashboard");
            Uri.UnescapeDataString(redirectResult.Url!).Should().NotContain("returnUrl=");
        }

        [Fact]
        public void Login_WithExternalReturnUrl_ReturnsValidationError()
        {
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);
            _mockConfiguration.Setup(x => x["OAuth:Authority"])
                .Returns("https://login.microsoftonline.com/tenant-id");
            _mockConfiguration.Setup(x => x["OAuth:ClientId"]).Returns("client-id");

            var result = _controller.Login("https://attacker.example/collect");

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().BeOfType<ApiResponse>();
            GetSessionString("oauth_state").Should().BeNull();
        }

        [Fact]
        public void Login_WithNullReturnUrl_ReturnsRedirectResult()
        {
            // Arrange
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            _mockConfiguration.Setup(x => x["OAuth:Authority"]).Returns("https://login.microsoftonline.com/tenant-id");
            _mockConfiguration.Setup(x => x["OAuth:ClientId"]).Returns("client-id");
            _mockConfiguration.Setup(x => x["OAuth:Scope"]).Returns("openid profile email");
            _mockConfiguration.Setup(x => x["OAuth:CallbackPath"]).Returns("/signin-oidc");
            _mockConfiguration.Setup(x => x["OAuth:ResponseType"]).Returns("code");

            // Act
            var result = _controller.Login(null);

            // Assert
            result.Should().BeOfType<RedirectResult>();
            var redirectResult = result as RedirectResult;
            redirectResult.Should().NotBeNull();
            redirectResult!.Url.Should().Contain("login.microsoftonline.com");
        }

        [Fact]
        public void Login_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            _mockConfiguration.Setup(x => x["OAuth:Authority"]).Throws(new Exception("Configuration error"));

            // Act
            var result = _controller.Login(null);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Callback_WithValidCode_PersistsUserAndRedirectsWithOpaqueCode()
        {
            // Arrange
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            var code = "valid-auth-code";
            var state = "valid-state";
            var returnUrl = "/dashboard";

            SetSessionString("oauth_state", state);
            SetSessionString("oauth_return_to", "/checkout");

            _mockConfiguration.Setup(x => x["OAuth:CallbackPath"]).Returns("/signin-oidc");

            var tokenResult = new OAuthTokenResult
            {
                AccessToken = "access-token",
                IdToken = "id-token",
                RefreshToken = "refresh-token"
            };

            var userInfo = new OAuthUserInfo
            {
                Subject = "user-subject",
                Email = "user@example.com",
                Name = "John Doe",
                GivenName = "John",
                FamilyName = "Doe",
                Picture = "https://example.com/picture.jpg"
            };

            _mockOAuthService.Setup(x => x.ExchangeCodeForTokenAsync(code, It.IsAny<string>()))
                           .ReturnsAsync(tokenResult);

            _mockOAuthService.Setup(x => x.GetUserInfoAsync(tokenResult.AccessToken))
                           .ReturnsAsync(userInfo);

            _mockJwtTokenGenerator.Setup(x => x.GenerateToken(It.IsAny<Ecommerce.Domain.Entities.ApplicationUser>()))
                                .Returns("jwt-token");
            var dbUser = new ApplicationUser
            {
                Id = 42,
                Username = "oauth:microsoft:user-subject",
                Email = "user@example.com",
                PasswordHash = "not-used",
                FirstName = "John",
                LastName = "Doe",
                Role = "User"
            };
            _mockExternalUserService
                .Setup(x => x.UpsertAsync(
                    "microsoft",
                    userInfo,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(dbUser);
            OAuthAuthorizationGrant? issuedGrant = null;
            _mockAuthorizationCodeStore
                .Setup(x => x.IssueAsync(
                    "microsoft",
                    It.IsAny<OAuthAuthorizationGrant>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, OAuthAuthorizationGrant, CancellationToken>(
                    (_, grant, _) => issuedGrant = grant)
                .ReturnsAsync("opaque-code");
            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(x => x.SignInAsync(
                    It.IsAny<HttpContext>(),
                    "Cookie",
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties?>()))
                .Returns(Task.CompletedTask);
            var services = new ServiceCollection()
                .AddSingleton(authenticationService.Object)
                .BuildServiceProvider();
            MockHttpContext.Setup(x => x.RequestServices).Returns(services);

            // Act
            var result = await _controller.Callback(code, state, null, returnUrl);

            // Assert
            var redirect = result.Should().BeOfType<RedirectResult>().Subject;
            redirect.Url.Should().Contain("code=opaque-code");
            redirect.Url.Should().Contain("provider=microsoft");
            redirect.Url.Should().NotContain("jwt-token");
            redirect.Url.Should().NotContain("jwt_token");
            issuedGrant.Should().NotBeNull();
            issuedGrant!.UserId.Should().Be(42);
            issuedGrant.ReturnTo.Should().Be("/checkout");
        }

        [Fact]
        public async Task Callback_WithError_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext(_controller);

            var error = "access_denied";
            var errorDescription = "User denied access";

            // Act
            var result = await _controller.Callback(null, null, error, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Exchange_WithSingleUseGrant_ReturnsStandardAuthEnvelope()
        {
            SetupControllerContext(_controller);
            _mockAuthorizationCodeStore
                .Setup(x => x.ConsumeAsync(
                    "microsoft",
                    "opaque-code",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OAuthAuthorizationGrant(
                    "jwt-token",
                    42,
                    "John",
                    "Doe",
                    "user@example.com",
                    "User",
                    null,
                    "/checkout",
                    DateTimeOffset.UtcNow.AddMinutes(1)));

            var result = await _controller.Exchange(
                new OAuthCodeExchangeRequest("opaque-code"),
                CancellationToken.None);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var envelope = ok.Value
                .Should().BeOfType<ApiResponse<OAuthCodeExchangeResponse>>().Subject;
            envelope.IsSuccessful.Should().BeTrue();
            envelope.Data!.Token.Should().Be("jwt-token");
            envelope.Data.UserId.Should().Be(42);
            envelope.Data.ReturnTo.Should().Be("/checkout");
        }

        [Fact]
        public async Task Callback_WithNullCode_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.Callback(null, "state", null, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Callback_WithInvalidState_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext(_controller);

            var code = "valid-auth-code";
            var state = "invalid-state";

            SetSessionString("oauth_state", "different-state");

            // Act
            var result = await _controller.Callback(code, state, null, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Callback_WithNullState_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext(_controller);

            var code = "valid-auth-code";

            SetSessionString("oauth_state", "stored-state");

            // Act
            var result = await _controller.Callback(code, null, null, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Callback_WithFailedTokenExchange_ReturnsBadRequest()
        {
            // Arrange
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            var code = "valid-auth-code";
            var state = "valid-state";

            SetSessionString("oauth_state", state);

            _mockConfiguration.Setup(x => x["OAuth:CallbackPath"]).Returns("/signin-oidc");

            var tokenResult = new OAuthTokenResult
            {
                AccessToken = null, // Failed token exchange
                IdToken = null,
                RefreshToken = null
            };

            _mockOAuthService.Setup(x => x.ExchangeCodeForTokenAsync(code, It.IsAny<string>()))
                           .ReturnsAsync(tokenResult);

            // Act
            var result = await _controller.Callback(code, state, null, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Callback_WithFailedUserInfo_ReturnsBadRequest()
        {
            // Arrange
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            var code = "valid-auth-code";
            var state = "valid-state";

            SetSessionString("oauth_state", state);

            _mockConfiguration.Setup(x => x["OAuth:CallbackPath"]).Returns("/signin-oidc");

            var tokenResult = new OAuthTokenResult
            {
                AccessToken = "access-token",
                IdToken = "id-token",
                RefreshToken = "refresh-token"
            };

            var userInfo = new OAuthUserInfo
            {
                Subject = "user-subject",
                Email = null, // Failed user info
                Name = "John Doe",
                GivenName = "John",
                FamilyName = "Doe",
                Picture = "https://example.com/picture.jpg"
            };

            _mockOAuthService.Setup(x => x.ExchangeCodeForTokenAsync(code, It.IsAny<string>()))
                           .ReturnsAsync(tokenResult);

            _mockOAuthService.Setup(x => x.GetUserInfoAsync(tokenResult.AccessToken))
                           .ReturnsAsync(userInfo);

            // Act
            var result = await _controller.Callback(code, state, null, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Callback_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            var code = "valid-auth-code";
            var state = "valid-state";

            SetSessionString("oauth_state", state);

            _mockOAuthService.Setup(x => x.ExchangeCodeForTokenAsync(code, It.IsAny<string>()))
                           .ThrowsAsync(new Exception("OAuth service error"));

            // Act
            var result = await _controller.Callback(code, state, null, null);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Logout_WithValidUser_ReturnsStandardApiResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            _mockConfiguration.Setup(x => x["OAuth:Authority"]).Returns("https://login.microsoftonline.com/tenant-id");
            _mockConfiguration.Setup(x => x["OAuth:ClientId"]).Returns("client-id");
            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(x => x.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    "Cookie",
                    It.IsAny<AuthenticationProperties?>()))
                .Returns(Task.CompletedTask);
            var services = new ServiceCollection()
                .AddSingleton(authenticationService.Object)
                .BuildServiceProvider();
            MockHttpContext.Setup(x => x.RequestServices).Returns(services);

            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value
                .Should().BeOfType<ApiResponse<OAuthLogoutResponse>>().Subject;
            response.IsSuccessful.Should().BeTrue();
            response.Data!.LogoutUrl.Should().Contain("login.microsoftonline.com");
        }

        [Fact]
        public async Task Logout_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            _mockConfiguration.Setup(x => x["OAuth:Authority"]).Throws(new Exception("Configuration error"));
            var authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(x => x.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    "Cookie",
                    It.IsAny<AuthenticationProperties?>()))
                .Returns(Task.CompletedTask);
            var services = new ServiceCollection()
                .AddSingleton(authenticationService.Object)
                .BuildServiceProvider();
            MockHttpContext.Setup(x => x.RequestServices).Returns(services);

            // Act
            var result = await _controller.Logout();

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public void GetUserInfo_WithValidUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1, "user@example.com", "User");
            SetupControllerContext(_controller);

            // Act
            var result = _controller.GetUserInfo();

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public void GetUserInfo_WithUnauthenticatedUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            // Act
            var result = _controller.GetUserInfo();

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public void GetUserInfo_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            MockUser.Setup(x => x.FindFirst(It.IsAny<string>())).Throws(new Exception("Claims error"));

            // Act
            var result = _controller.GetUserInfo();

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }
    }
}
