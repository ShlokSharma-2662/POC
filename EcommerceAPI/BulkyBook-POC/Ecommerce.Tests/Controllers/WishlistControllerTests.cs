using Ecommerce.API.Controllers;
using Ecommerce.Application.Features.Wishlist.Commands;
using Ecommerce.Application.Features.Wishlist.Models;
using Ecommerce.Application.Features.Wishlist.Queries;
using Ecommerce.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Ecommerce.Tests.Controllers
{
    public class WishlistControllerTests : ControllerTestBase
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly WishlistController _controller;

        public WishlistControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new WishlistController(_mockMediator.Object);
            SetupHttpContext();
        }

        [Fact]
        public async Task GetUserWishlist_WithValidUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var expectedResult = new List<WishlistItemDto>
            {
                new WishlistItemDto
                {
                    ProductId = 1,
                    ProductName = "Test Product"
                }
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserWishlistQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetUserWishlist();

            // Assert
            result.Should().BeOfType<ActionResult<List<WishlistItemDto>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetUserWishlist_WithInvalidUserToken_ReturnsUnauthorizedResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.GetUserWishlist();

            // Assert
            result.Should().BeOfType<ActionResult<List<WishlistItemDto>>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task GetUserWishlist_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserWishlistQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUserWishlist();

            // Assert
            result.Should().BeOfType<ActionResult<List<WishlistItemDto>>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task AddToWishlist_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new AddToWishlistRequest
            {
                ProductId = 1
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<AddToWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.AddToWishlist(request);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task AddToWishlist_WithFailedResult_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new AddToWishlistRequest
            {
                ProductId = 1
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<AddToWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // Act
            var result = await _controller.AddToWishlist(request);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task AddToWishlist_WithInvalidUserToken_ReturnsUnauthorizedResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            var request = new AddToWishlistRequest
            {
                ProductId = 1
            };

            // Act
            var result = await _controller.AddToWishlist(request);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task AddToWishlist_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new AddToWishlistRequest
            {
                ProductId = 1
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<AddToWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.AddToWishlist(request);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task RemoveFromWishlist_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new RemoveFromWishlistRequest
            {
                ProductId = 1
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<RemoveFromWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.RemoveFromWishlist(request);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task RemoveFromWishlist_WithFailedResult_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new RemoveFromWishlistRequest
            {
                ProductId = 1
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<RemoveFromWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // Act
            var result = await _controller.RemoveFromWishlist(request);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task RemoveFromWishlist_WithInvalidUserToken_ReturnsUnauthorizedResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            var request = new RemoveFromWishlistRequest
            {
                ProductId = 1
            };

            // Act
            var result = await _controller.RemoveFromWishlist(request);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task RemoveFromWishlist_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new RemoveFromWishlistRequest
            {
                ProductId = 1
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<RemoveFromWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.RemoveFromWishlist(request);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CheckWishlistStatus_WithValidProductId_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var productId = 1;

            _mockMediator.Setup(x => x.Send(It.IsAny<CheckWishlistStatusQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.CheckWishlistStatus(productId);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task CheckWishlistStatus_WithInvalidUserToken_ReturnsUnauthorizedResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            var productId = 1;

            // Act
            var result = await _controller.CheckWishlistStatus(productId);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task CheckWishlistStatus_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var productId = 1;

            _mockMediator.Setup(x => x.Send(It.IsAny<CheckWishlistStatusQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CheckWishlistStatus(productId);

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetWishlistCount_WithValidUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var expectedWishlistItems = new List<WishlistItemDto>
            {
                new WishlistItemDto { ProductId = 1 },
                new WishlistItemDto { ProductId = 2 }
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserWishlistQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedWishlistItems);

            // Act
            var result = await _controller.GetWishlistCount();

            // Assert
            result.Should().BeOfType<ActionResult<int>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetWishlistCount_WithInvalidUserToken_ReturnsUnauthorizedResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.GetWishlistCount();

            // Assert
            result.Should().BeOfType<ActionResult<int>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task GetWishlistCount_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<GetUserWishlistQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetWishlistCount();

            // Assert
            result.Should().BeOfType<ActionResult<int>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ClearWishlist_WithValidUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<ClearWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.ClearWishlist();

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ClearWishlist_WithFailedResult_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<ClearWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            // Act
            var result = await _controller.ClearWishlist();

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ClearWishlist_WithInvalidUserToken_ReturnsUnauthorizedResponse()
        {
            // Arrange
            SetupUnauthenticatedUser();
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.ClearWishlist();

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var unauthorizedResult = result.Result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task ClearWishlist_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<ClearWishlistCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.ClearWishlist();

            // Assert
            result.Should().BeOfType<ActionResult<bool>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }
    }
}
