using Ecommerce.API.Controllers;
using Ecommerce.Application.Features.Orders.Commands;
using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Application.Features.Orders.Queries;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Tests.Common;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrderItemDto = Ecommerce.Application.Features.Orders.Commands.OrderItemDto;

namespace Ecommerce.Tests.Controllers
{
    public class OrdersControllerTests : ControllerTestBase
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            var messagePublisherMock = new Mock<IMessagePublisherService>();
            var loggerMock = new Mock<ILogger<OrdersController>>();
            _controller = new OrdersController(_mockMediator.Object, messagePublisherMock.Object, loggerMock.Object);
            SetupHttpContext();
        }

        [Fact]
        public async Task Checkout_WithValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            var orderId = Guid.NewGuid();

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(orderId);

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Checkout_WithNullCommand_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.Checkout(null);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Checkout_WithEmptyItems_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = new List<OrderItemDto>() // Empty items
            };

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Checkout_WithNullItems_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = null // Null items
            };

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Checkout_WithMissingFullName_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "", // Empty full name
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Checkout_WithMissingAddress_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "", // Empty address
                PhoneNumber = "555-1234",
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Checkout_WithMissingPhoneNumber_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "", // Empty phone number
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Checkout_WithWhitespaceOnlyFields_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "   ", // Whitespace only
                Address = "   ", // Whitespace only
                PhoneNumber = "   ", // Whitespace only
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Checkout_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Checkout_WithArgumentException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Checkout_WithInvalidOperationException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task Checkout_WithTimeoutException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = new List<Ecommerce.Application.Features.Orders.Commands.OrderItemDto>
                {
                    new Ecommerce.Application.Features.Orders.Commands.OrderItemDto
                    {
                        ProductId = 1,
                        Quantity = 2
                    }
                }
            };

            _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new TimeoutException("Request timed out"));

            // Act
            var result = await _controller.Checkout(command);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetMyOrders_WithValidUser_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var expectedResult = new List<OrderDto>
            {
                new OrderDto
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "John Doe",
                    ShippingAddress = "123 Main St",
                    Phone = "555-1234",
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                }
            };

            _mockMediator.Setup(x => x.Send(It.IsAny<GetMyOrdersQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetMyOrders();

            // Assert
            result.Should().BeOfType<ActionResult<List<OrderDto>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetMyOrders_WithEmptyResult_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var expectedResult = new List<OrderDto>();

            _mockMediator.Setup(x => x.Send(It.IsAny<GetMyOrdersQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetMyOrders();

            // Assert
            result.Should().BeOfType<ActionResult<List<OrderDto>>>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetMyOrders_WithException_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            _mockMediator.Setup(x => x.Send(It.IsAny<GetMyOrdersQuery>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetMyOrders();

            // Assert
            result.Should().BeOfType<ActionResult<List<OrderDto>>>();
            var statusResult = result.Result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }
    }
}
