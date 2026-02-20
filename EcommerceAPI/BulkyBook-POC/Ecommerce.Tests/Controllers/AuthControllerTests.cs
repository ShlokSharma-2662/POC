using Ecommerce.API.Controllers;
using Ecommerce.Application.Features.Auth.Command;
using Ecommerce.Application.Features.Auth.Commands;
using Ecommerce.Application.Features.Auth.Models;
using Ecommerce.Application.Features.Auth.Queries;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Ecommerce.Tests.Controllers
{
    public class AuthControllerTests : ControllerTestBase
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            var messagePublisherMock = new Mock<IMessagePublisherService>();
            _controller = new AuthController(_mockMediator.Object, messagePublisherMock.Object);
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
        public async Task Register_WithValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };

            var expectedResult = new AuthResult(
                userId: 1,
                firstName: "John",
                lastName: "Doe",
                email: "test@example.com",
                role: "User",
                token: "jwt-token"
            );

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.Register(command);

            // Assert
            result.Should().BeOfType<ActionResult<AuthResult>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Register_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Registration failed"));

            // Act
            var result = await _controller.Register(command);

            // Assert
            result.Should().BeOfType<ActionResult<AuthResult>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Login_WithValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var expectedResult = new AuthResult(
                userId: 1,
                firstName: "John",
                lastName: "Doe",
                email: "test@example.com",
                role: "User",
                token: "jwt-token"
            );

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.Login(command);

            // Assert
            result.Should().BeOfType<ActionResult<AuthResult>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Login_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Invalid credentials"));

            // Act
            var result = await _controller.Login(command);

            // Assert
            result.Should().BeOfType<ActionResult<AuthResult>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetProfile_WithValidUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1, "test@example.com");
            SetupControllerContext(_controller);

            var expectedProfile = new UserProfileDto
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedProfile);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            result.Should().BeOfType<ActionResult<UserProfileDto>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetProfile_WithInvalidUserToken_ReturnsUnauthorizedResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            result.Should().BeOfType<ActionResult<UserProfileDto>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task GetProfile_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetProfile();

            // Assert
            result.Should().BeOfType<ActionResult<UserProfileDto>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ChangePassword_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            AssertSuccessResponse(result);
        }

        [Fact]
        public async Task ChangePassword_WithNullDto_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.ChangePassword(null);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ChangePassword_WithEmptyPasswords_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var dto = new ChangePasswordDto
            {
                OldPassword = "",
                NewPassword = "",
                ConfirmPassword = ""
            };

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ChangePassword_WithInvalidUserToken_ReturnsUnauthorizedResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task ChangePassword_WithFailedResult_ReturnsSuccessResponseWithFailureMessage()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ChangePassword_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var dto = new ChangePasswordDto
            {
                OldPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            result.Should().BeOfType<ActionResult<object>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }
    }
}
