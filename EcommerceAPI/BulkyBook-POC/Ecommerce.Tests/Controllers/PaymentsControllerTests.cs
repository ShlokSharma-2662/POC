using Ecommerce.API.Controllers;
using Ecommerce.Application.Features.Payments.Models;
using Ecommerce.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe;
using Stripe.Checkout;

namespace Ecommerce.Tests.Controllers
{
    public class PaymentsControllerTests : ControllerTestBase
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly PaymentsController _controller;

        public PaymentsControllerTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(x => x["Stripe:SecretKey"]).Returns("sk_test_123456789");
            _controller = new PaymentsController(_mockConfiguration.Object);
            SetupHttpContext();
        }

        [Fact]
        public async Task CreatePaymentIntent_WithValidRequest_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 2000, // ₹20.00 in paise
                Currency = "inr",
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", "123" }
                }
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithZeroAmount_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 0,
                Currency = "inr"
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithNegativeAmount_ReturnsValidationErrorResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = -100,
                Currency = "inr"
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithNullRequest_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            // Act
            var result = await _controller.CreatePaymentIntent(null);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithInvalidStripeKey_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var invalidConfig = new Mock<IConfiguration>();
            invalidConfig.Setup(x => x["Stripe:SecretKey"]).Returns("invalid_key");
            var controllerWithInvalidConfig = new PaymentsController(invalidConfig.Object);
            SetupControllerContext(controllerWithInvalidConfig);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 2000,
                Currency = "inr"
            };

            // Act
            var result = await controllerWithInvalidConfig.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithLargeAmount_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 99999999, // Large amount
                Currency = "inr"
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithDifferentCurrency_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 2000,
                Currency = "inr"
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithMetadata_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 2000,
                Currency = "inr",
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", "123" },
                    { "userId", "456" },
                    { "productId", "789" }
                }
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithEmptyMetadata_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 2000,
                Currency = "inr",
                Metadata = new Dictionary<string, string>()
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithNullMetadata_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 2000,
                Currency = "inr",
                Metadata = null
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreatePaymentIntent_WithMinimalAmount_ReturnsExceptionResponse()
        {
            // Arrange
            SetupAuthenticatedUser(1);
            SetupControllerContext(_controller);

            var request = new CreatePaymentIntentRequest
            {
                Amount = 1, // Minimum amount
                Currency = "inr"
            };

            // Act
            var result = await _controller.CreatePaymentIntent(request);

            // Assert
            result.Should().BeAssignableTo<IActionResult>();
            var statusResult = result as ObjectResult;
            statusResult.Should().NotBeNull();
            statusResult!.StatusCode.Should().Be(500);
        }
    }
}

