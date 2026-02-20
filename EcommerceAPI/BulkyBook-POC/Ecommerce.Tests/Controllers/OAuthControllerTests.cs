using Ecommerce.API.Controllers;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly OAuthController _controller;

        public OAuthControllerTests()
        {
            _mockOAuthService = new Mock<IOAuthService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<OAuthController>>();
            _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
            _controller = new OAuthController(_mockOAuthService.Object, _mockConfiguration.Object, _mockLogger.Object, _mockJwtTokenGenerator.Object);
            SetupHttpContext();
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
        public async Task Callback_WithValidCode_ReturnsExceptionResponse()
        {
            // Arrange
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            var code = "valid-auth-code";
            var state = "valid-state";
            var returnUrl = "/dashboard";

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

            // Act
            var result = await _controller.Callback(code, state, null, returnUrl);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(400);
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
        public async Task Logout_WithValidUser_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupRequestScheme();
            SetupRequestHost();
            SetupControllerContext(_controller);

            _mockConfiguration.Setup(x => x["OAuth:Authority"]).Returns("https://login.microsoftonline.com/tenant-id");
            _mockConfiguration.Setup(x => x["OAuth:ClientId"]).Returns("client-id");

            // Act
            var result = await _controller.Logout();

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(400);
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

            // Act
            var result = await _controller.Logout();

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(400);
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
