using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain.Payments;
using Ecommerce.Infrastructure.Payments;
using Ecommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ecommerce.Tests.Infrastructure;

public class SecureCheckoutPaymentServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly SecureCheckoutPaymentService _service;

    public SecureCheckoutPaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        _context = new AppDbContext(options);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:Currency"] = "usd"
            })
            .Build();
        _service = new SecureCheckoutPaymentService(
            _context,
            _gateway.Object,
            configuration,
            Mock.Of<ILogger<SecureCheckoutPaymentService>>());
    }

    [Fact]
    public async Task CreatePaymentIntent_PricesCurrentProductsOnServer()
    {
        AddProduct(7, 12.99m, stock: 10);
        await _context.SaveChangesAsync();
        PaymentIntentCreation? captured = null;
        _gateway
            .Setup(gateway => gateway.CreatePaymentIntentAsync(
                It.IsAny<PaymentIntentCreation>(),
                It.IsAny<CancellationToken>()))
            .Callback<PaymentIntentCreation, CancellationToken>(
                (request, _) => captured = request)
            .ReturnsAsync(() => new PaymentIntentSnapshot(
                "pi_server_priced",
                "pi_server_priced_secret",
                captured!.AmountMinor,
                captured.Currency,
                "requires_payment_method",
                captured.Metadata,
                false));

        var result = await _service.CreatePaymentIntentAsync(
            42,
            new[] { new PaymentCartItem(7, 2) },
            "256a2247-5b53-4d5a-84a5-5d5cd3791868",
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.AmountMinor.Should().Be(2598);
        captured.Currency.Should().Be("usd");
        captured.Metadata["user_id"].Should().Be("42");
        captured.Metadata["cart_fingerprint"].Should().Be(result.CartFingerprint);
        result.AmountMinor.Should().Be(2598);
    }

    [Fact]
    public async Task VerifyCheckoutPayment_RejectsAmountThatDoesNotMatchServerPrice()
    {
        AddProduct(7, 12.99m, stock: 10);
        await _context.SaveChangesAsync();
        var items = new[] { new PaymentCartItem(7, 2) };
        var fingerprint = SecureCheckoutPaymentService.ComputeFingerprint(42, items);
        _gateway
            .Setup(gateway => gateway.GetPaymentIntentAsync(
                "pi_wrong_amount",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntentSnapshot(
                "pi_wrong_amount",
                "secret",
                1,
                "usd",
                "succeeded",
                new Dictionary<string, string>
                {
                    ["user_id"] = "42",
                    ["cart_fingerprint"] = fingerprint
                },
                false));

        var action = () => _service.VerifyCheckoutPaymentAsync(
            42,
            "pi_wrong_amount",
            items,
            CancellationToken.None);

        await action.Should().ThrowAsync<PaymentRejectedException>()
            .WithMessage("*amount or currency*");
    }

    [Fact]
    public async Task VerifyCheckoutPayment_ReturnsSameOwnedOrderWithoutCallingStripe()
    {
        var fingerprint = SecureCheckoutPaymentService.ComputeFingerprint(
            42,
            new[] { new PaymentCartItem(7, 2) });
        var existingOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = "Test",
            ShippingAddress = "Address",
            Phone = "1234567890",
            UserId = 42,
            PaymentIntentId = "pi_replay",
            CartFingerprint = fingerprint,
            TotalAmount = 25.98m,
            Currency = "usd",
            CreatedAt = DateTime.UtcNow
        };
        _context.Orders.Add(existingOrder);
        await _context.SaveChangesAsync();

        var result = await _service.VerifyCheckoutPaymentAsync(
            42,
            "pi_replay",
            new[] { new PaymentCartItem(7, 2) },
            CancellationToken.None);

        result.ExistingOrderId.Should().Be(existingOrder.Id);
        _gateway.Verify(
            gateway => gateway.GetPaymentIntentAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyCheckoutPayment_RejectsReplayedPaymentFromAnotherUser()
    {
        var originalFingerprint = SecureCheckoutPaymentService.ComputeFingerprint(
            42,
            new[] { new PaymentCartItem(7, 2) });
        _context.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = "Test",
            ShippingAddress = "Address",
            Phone = "1234567890",
            UserId = 42,
            PaymentIntentId = "pi_owned",
            CartFingerprint = originalFingerprint,
            TotalAmount = 25.98m,
            Currency = "usd",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var action = () => _service.VerifyCheckoutPaymentAsync(
            99,
            "pi_owned",
            new[] { new PaymentCartItem(7, 2) },
            CancellationToken.None);

        await action.Should().ThrowAsync<PaymentRejectedException>();
    }

    [Fact]
    public async Task VerifyCheckoutPayment_RejectsRefundedIntent()
    {
        AddProduct(7, 10m, stock: 10);
        await _context.SaveChangesAsync();
        var items = new[] { new PaymentCartItem(7, 1) };
        var fingerprint = SecureCheckoutPaymentService.ComputeFingerprint(42, items);
        _gateway
            .Setup(gateway => gateway.GetPaymentIntentAsync(
                "pi_refunded",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentIntentSnapshot(
                "pi_refunded",
                "secret",
                1000,
                "usd",
                "succeeded",
                new Dictionary<string, string>
                {
                    ["user_id"] = "42",
                    ["cart_fingerprint"] = fingerprint
                },
                true));

        var action = () => _service.VerifyCheckoutPaymentAsync(
            42,
            "pi_refunded",
            items,
            CancellationToken.None);

        await action.Should().ThrowAsync<PaymentRejectedException>()
            .WithMessage("*refunded*");
    }

    private void AddProduct(int id, decimal price, int stock)
    {
        _context.Products.Add(new Product
        {
            ProductId = id,
            Name = $"Product {id}",
            Description = "Test",
            Price = price,
            Stock = stock,
            IsActive = true,
            IsDeleted = false,
            CategoryId = 1
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
