using Ecommerce.API.Controllers;
using Ecommerce.Application.Features.Cart.Commands;
using Ecommerce.Application.Features.Cart.Models;
using Ecommerce.Application.Features.Cart.Queries;
using Ecommerce.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Ecommerce.Tests.Controllers
{
    public class CartControllerTests : ControllerTestBase
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new CartController(_mockMediator.Object);
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
        public async Task GetCart_WithValidUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var expectedResult = new List<CartItemDto>
            {
                new CartItemDto
                {
                    ProductId = 1,
                    Quantity = 2,
                    Product = new ProductBriefDto
                    {
                        ProductId = 1,
                        Name = "Test Product",
                        Price = 100
                    }
                }
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetMyCartQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetCart();

            // Assert
            result.Should().BeOfType<ActionResult<List<CartItemDto>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetCart_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<GetMyCartQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetCart();

            // Assert
            result.Should().BeOfType<ActionResult<List<CartItemDto>>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task AddToCart_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new AddToCartRequest(1, 2);

            var expectedResult = new AddToCartResult
            {
                Success = true,
                Message = "Product added to cart successfully"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<AddToCartCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.AddToCart(request);

            // Assert
            result.Should().BeOfType<ActionResult<AddToCartResult>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task AddToCart_WithFailedResult_ReturnsErrorResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new AddToCartRequest(1, 2);

            var expectedResult = new AddToCartResult
            {
                Success = false,
                Message = "Product not available"
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<AddToCartCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.AddToCart(request);

            // Assert
            result.Should().BeOfType<ActionResult<AddToCartResult>>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task AddToCart_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new AddToCartRequest(1, 2);

            _mockMediator.Setup(x => x.Send(It.IsAny<AddToCartCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.AddToCart(request);

            // Assert
            result.Should().BeOfType<ActionResult<AddToCartResult>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateCart_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new UpdateCartRequest(1, 3);

            _mockMediator.Setup(m => m.Send(It.IsAny<UpdateCartCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.UpdateCart(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
            
            _mockMediator.Verify(m => m.Send(
                It.IsAny<UpdateCartCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCart_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new UpdateCartRequest(1, 3);

            _mockMediator.Setup(x => x.Send(It.IsAny<UpdateCartCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateCart(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteFromCart_WithValidProductId_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var productId = 1;

            _mockMediator
              .Setup(m => m.Send(It.IsAny<DeleteCartItemCommand>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.DeleteFromCart(productId);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task DeleteFromCart_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var productId = 1;

            _mockMediator.Setup(x => x.Send(It.IsAny<DeleteCartItemCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteFromCart(productId);

            // Assert
            result.Should().BeAssignableTo<ActionResult>();
            result.Should().BeOfType<ObjectResult>();
            var statusResult = result as ObjectResult;
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ClearCart_WithValidUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator
                 .Setup(m => m.Send(It.IsAny<ClearCartCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.ClearCart();

            // Assert
            result.Should().BeAssignableTo<ActionResult>();
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ClearCart_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<ClearCartCommand>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.ClearCart();

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }
    }
}
