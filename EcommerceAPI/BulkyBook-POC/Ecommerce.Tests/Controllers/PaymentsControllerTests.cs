using Ecommerce.API.Controllers;
using Ecommerce.Application.Features.Payments.Models;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain.Payments;
using Ecommerce.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ecommerce.Tests.Controllers;

public class PaymentsControllerTests : ControllerTestBase
{
    private readonly Mock<ISecureCheckoutPaymentService> _paymentService = new();
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _controller = new PaymentsController(
            _paymentService.Object,
            Mock.Of<ILogger<PaymentsController>>());
        SetupHttpContext();
    }

    [Fact]
    public async Task CreatePaymentIntent_UsesAuthenticatedUserAndCart_NotClientPricing()
    {
        SetupAuthenticatedUser(42);
        SetupControllerContext(_controller);
        _paymentService
            .Setup(service => service.CreatePaymentIntentAsync(
                42,
                It.Is<IReadOnlyCollection<PaymentCartItem>>(items =>
                    items.Count == 1 &&
                    items.Single().ProductId == 7 &&
                    items.Single().Quantity == 2),
                "256a2247-5b53-4d5a-84a5-5d5cd3791868",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurePaymentIntentResult(
                "pi_secure",
                "pi_secure_secret",
                2598,
                "usd",
                "fingerprint"));

        var result = await _controller.CreatePaymentIntent(
            new CreatePaymentIntentRequest
            {
                CheckoutReference = "256a2247-5b53-4d5a-84a5-5d5cd3791868",
                Items =
                [
                    new CreatePaymentIntentItemRequest
                    {
                        ProductId = 7,
                        Quantity = 2
                    }
                ]
            },
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _paymentService.VerifyAll();
    }

    [Fact]
    public async Task CreatePaymentIntent_WithoutItems_IsRejectedBeforeStripe()
    {
        SetupAuthenticatedUser(42);
        SetupControllerContext(_controller);

        var result = await _controller.CreatePaymentIntent(
            new CreatePaymentIntentRequest(),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _paymentService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePaymentIntent_WithoutAuthenticatedSubject_IsRejected()
    {
        SetupUnauthenticatedUser();
        SetupControllerContext(_controller);

        var result = await _controller.CreatePaymentIntent(
            new CreatePaymentIntentRequest
            {
                Items =
                [
                    new CreatePaymentIntentItemRequest
                    {
                        ProductId = 7,
                        Quantity = 2
                    }
                ]
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        _paymentService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePaymentIntent_WithInvalidServerCart_ReturnsBadRequest()
    {
        SetupAuthenticatedUser(42);
        SetupControllerContext(_controller);
        _paymentService
            .Setup(service => service.CreatePaymentIntentAsync(
                It.IsAny<long>(),
                It.IsAny<IReadOnlyCollection<PaymentCartItem>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentValidationException("Product 7 is unavailable."));

        var result = await _controller.CreatePaymentIntent(
            new CreatePaymentIntentRequest
            {
                Items =
                [
                    new CreatePaymentIntentItemRequest
                    {
                        ProductId = 7,
                        Quantity = 2
                    }
                ]
            },
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
