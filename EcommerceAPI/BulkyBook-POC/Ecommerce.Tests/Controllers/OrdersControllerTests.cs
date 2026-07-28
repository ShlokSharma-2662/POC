using Ecommerce.OrderService.Controllers;
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
using Moq.Protected;
using Ecommerce.Infrastructure.Services;
using Ecommerce.Application.Common.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading;
using OrderItemDto = Ecommerce.Application.Features.Orders.Commands.OrderItemDto;
using Ecommerce.Domain.Payments;

namespace Ecommerce.Tests.Controllers
{
    public class OrdersControllerTests : ControllerTestBase
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<IEventGridPublisherService> _mockEventGridPublisher;
        private readonly Mock<IUserContextService> _mockUserContext;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ISecureCheckoutPaymentService> _mockPaymentService;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _mockEventGridPublisher = new Mock<IEventGridPublisherService>();
            _mockUserContext = new Mock<IUserContextService>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockPaymentService = new Mock<ISecureCheckoutPaymentService>();
            _mockPaymentService
                .Setup(x => x.VerifyCheckoutPaymentAsync(
                    1,
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyCollection<PaymentCartItem>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VerifiedCheckoutPayment(
                    "pi_test",
                    20000,
                    200m,
                    "usd",
                    "fingerprint",
                    null));
            _mockPaymentService
                .Setup(x => x.RefundAfterOrderFailureAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var messagePublisherMock = new Mock<IMessagePublisherService>();
            var loggerMock = new Mock<ILogger<OrdersController>>();

            _controller = new OrdersController(
                _mockMediator.Object,
                messagePublisherMock.Object,
                _mockEventGridPublisher.Object,
                _mockHttpClientFactory.Object,
                _mockUserContext.Object,
                _mockPaymentService.Object,
                _mockConfiguration.Object,
                loggerMock.Object);

            SetupHttpContext();
        }

        [Fact]
        public async Task Checkout_WithValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);
 
            _mockUserContext.Setup(x => x.GetCurrentUserId()).Returns(1L);
 
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Moq.Protected.ItExpr.IsAny<HttpRequestMessage>(),
                    Moq.Protected.ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
                });
 
            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                PaymentIntentId = "pi_test",
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
                PaymentIntentId = "pi_test",
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
                PaymentIntentId = "pi_test",
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
                PaymentIntentId = "pi_test",
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
                PaymentIntentId = "pi_test",
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
                PaymentIntentId = "pi_test",
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
                PaymentIntentId = "pi_test",
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
                PaymentIntentId = "pi_test",
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
        public async Task Checkout_WithoutPaymentIntent_IsRejectedBeforeOrderCreation()
        {
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);
            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                Items = new List<OrderItemDto>
                {
                    new() { ProductId = 1, Quantity = 1 }
                }
            };

            var result = await _controller.Checkout(command);

            result.Should().BeOfType<BadRequestObjectResult>();
            _mockMediator.Verify(
                mediator => mediator.Send(
                    It.IsAny<CheckoutOrderCommand>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Checkout_ReplayedOwnedPayment_ReturnsExistingOrder()
        {
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);
            var existingOrderId = Guid.NewGuid();
            _mockPaymentService
                .Setup(service => service.VerifyCheckoutPaymentAsync(
                    1,
                    "pi_replay",
                    It.IsAny<IReadOnlyCollection<PaymentCartItem>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VerifiedCheckoutPayment(
                    "pi_replay",
                    10000,
                    100m,
                    "usd",
                    "fingerprint",
                    existingOrderId));
            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                PaymentIntentId = "pi_replay",
                Items = new List<OrderItemDto>
                {
                    new() { ProductId = 1, Quantity = 1 }
                }
            };

            var result = await _controller.Checkout(command);

            result.Should().BeOfType<OkObjectResult>();
            _mockMediator.Verify(
                mediator => mediator.Send(
                    It.IsAny<CheckoutOrderCommand>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            _mockPaymentService.Verify(
                service => service.RefundAfterOrderFailureAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Checkout_WhenConcurrentRequestWins_ReturnsWinnerWithoutRefund()
        {
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);
            var winnerOrderId = Guid.NewGuid();
            _mockPaymentService
                .SetupSequence(service => service.VerifyCheckoutPaymentAsync(
                    1,
                    "pi_race",
                    It.IsAny<IReadOnlyCollection<PaymentCartItem>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VerifiedCheckoutPayment(
                    "pi_race",
                    10000,
                    100m,
                    "usd",
                    "fingerprint",
                    null))
                .ReturnsAsync(new VerifiedCheckoutPayment(
                    "pi_race",
                    10000,
                    100m,
                    "usd",
                    "fingerprint",
                    winnerOrderId));
            _mockMediator
                .Setup(mediator => mediator.Send(
                    It.IsAny<CheckoutOrderCommand>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Unique index loser"));
            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                PaymentIntentId = "pi_race",
                Items = new List<OrderItemDto>
                {
                    new() { ProductId = 1, Quantity = 1 }
                }
            };

            var result = await _controller.Checkout(command);

            result.Should().BeOfType<OkObjectResult>();
            _mockPaymentService.Verify(
                service => service.RefundAfterOrderFailureAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Checkout_WhenOrderPersistenceFails_RefundsSuccessfulPayment()
        {
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);
            _mockMediator
                .Setup(mediator => mediator.Send(
                    It.IsAny<CheckoutOrderCommand>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database failure"));
            var command = new CheckoutOrderCommand
            {
                FullName = "John Doe",
                Address = "123 Main St",
                PhoneNumber = "555-1234",
                PaymentIntentId = "pi_refund",
                Items = new List<OrderItemDto>
                {
                    new() { ProductId = 1, Quantity = 1 }
                }
            };

            var result = await _controller.Checkout(command);

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            _mockPaymentService.Verify(
                service => service.RefundAfterOrderFailureAsync(
                    "pi_refund",
                    It.IsAny<CancellationToken>()),
                Times.Once);
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
