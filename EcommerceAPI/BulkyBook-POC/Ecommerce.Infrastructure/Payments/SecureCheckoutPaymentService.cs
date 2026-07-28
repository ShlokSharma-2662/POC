using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain.Payments;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Payments;

public sealed class SecureCheckoutPaymentService : ISecureCheckoutPaymentService
{
    private const int MaximumQuantityPerProduct = 1000;
    private readonly AppDbContext _context;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<SecureCheckoutPaymentService> _logger;
    private readonly string _currency;

    public SecureCheckoutPaymentService(
        AppDbContext context,
        IPaymentGateway paymentGateway,
        IConfiguration configuration,
        ILogger<SecureCheckoutPaymentService> logger)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _logger = logger;
        _currency = NormalizeCurrency(configuration["Stripe:Currency"]);
    }

    public async Task<SecurePaymentIntentResult> CreatePaymentIntentAsync(
        long userId,
        IReadOnlyCollection<PaymentCartItem> items,
        string? checkoutReference,
        CancellationToken cancellationToken)
    {
        var normalizedItems = NormalizeItems(items);
        var pricing = await PriceCartAsync(normalizedItems, cancellationToken);
        var fingerprint = ComputeFingerprint(userId, normalizedItems);
        var reference = NormalizeCheckoutReference(checkoutReference);

        var intent = await _paymentGateway.CreatePaymentIntentAsync(
            new PaymentIntentCreation(
                pricing.AmountMinor,
                _currency,
                new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(CultureInfo.InvariantCulture),
                    ["cart_fingerprint"] = fingerprint,
                    ["checkout_reference"] = reference
                },
                $"checkout:{userId}:{reference}"),
            cancellationToken);

        return new SecurePaymentIntentResult(
            intent.Id,
            intent.ClientSecret,
            pricing.AmountMinor,
            _currency,
            fingerprint);
    }

    public async Task<VerifiedCheckoutPayment> VerifyCheckoutPaymentAsync(
        long userId,
        string paymentIntentId,
        IReadOnlyCollection<PaymentCartItem> items,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId) ||
            !paymentIntentId.StartsWith("pi_", StringComparison.Ordinal))
        {
            throw new PaymentValidationException("A valid PaymentIntent ID is required.");
        }

        var normalizedItems = NormalizeItems(items);
        var fingerprint = ComputeFingerprint(userId, normalizedItems);

        var existingOrder = await _context.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                order => order.PaymentIntentId == paymentIntentId,
                cancellationToken);

        if (existingOrder != null)
        {
            if (existingOrder.UserId != userId ||
                !FixedTimeEquals(existingOrder.CartFingerprint, fingerprint))
            {
                throw new PaymentRejectedException(
                    "This payment is already linked to a different order.");
            }

            return new VerifiedCheckoutPayment(
                paymentIntentId,
                ToMinorUnits(existingOrder.TotalAmount),
                existingOrder.TotalAmount,
                existingOrder.Currency,
                existingOrder.CartFingerprint,
                existingOrder.Id);
        }

        var pricing = await PriceCartAsync(normalizedItems, cancellationToken);
        var intent = await _paymentGateway.GetPaymentIntentAsync(
            paymentIntentId,
            cancellationToken);

        if (!string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentRejectedException("The payment has not succeeded.");
        }

        if (intent.IsRefunded)
        {
            throw new PaymentRejectedException("The payment has already been refunded.");
        }

        if (intent.AmountMinor != pricing.AmountMinor ||
            !string.Equals(intent.Currency, _currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new PaymentRejectedException(
                "The payment amount or currency does not match the current server price.");
        }

        if (!intent.Metadata.TryGetValue("user_id", out var intentUserId) ||
            !FixedTimeEquals(intentUserId, userId.ToString(CultureInfo.InvariantCulture)) ||
            !intent.Metadata.TryGetValue("cart_fingerprint", out var intentFingerprint) ||
            !FixedTimeEquals(intentFingerprint, fingerprint))
        {
            throw new PaymentRejectedException(
                "The payment is not bound to this user and cart.");
        }

        return new VerifiedCheckoutPayment(
            paymentIntentId,
            pricing.AmountMinor,
            pricing.TotalAmount,
            _currency,
            fingerprint,
            ExistingOrderId: null);
    }

    public Task RefundAfterOrderFailureAsync(
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Compensating successful payment {PaymentIntentId} because order persistence failed.",
            paymentIntentId);
        return _paymentGateway.RefundPaymentIntentAsync(
            paymentIntentId,
            "order_persistence_failed",
            cancellationToken);
    }

    internal static IReadOnlyList<PaymentCartItem> NormalizeItems(
        IReadOnlyCollection<PaymentCartItem> items)
    {
        if (items == null || items.Count == 0)
        {
            throw new PaymentValidationException("The cart must contain at least one item.");
        }

        if (items.Any(item =>
                item.ProductId <= 0 ||
                item.Quantity <= 0 ||
                item.Quantity > MaximumQuantityPerProduct))
        {
            throw new PaymentValidationException(
                $"Product IDs must be positive and quantities must be between 1 and {MaximumQuantityPerProduct}.");
        }

        var normalized = items
            .GroupBy(item => item.ProductId)
            .Select(group => new PaymentCartItem(
                group.Key,
                checked(group.Sum(item => item.Quantity))))
            .OrderBy(item => item.ProductId)
            .ToArray();

        if (normalized.Any(item => item.Quantity > MaximumQuantityPerProduct))
        {
            throw new PaymentValidationException(
                $"Combined quantity cannot exceed {MaximumQuantityPerProduct} per product.");
        }

        return normalized;
    }

    public static string ComputeFingerprint(
        long userId,
        IReadOnlyCollection<PaymentCartItem> normalizedItems)
    {
        var canonical = string.Join(
            ";",
            normalizedItems
                .OrderBy(item => item.ProductId)
                .Select(item => $"{item.ProductId}:{item.Quantity}"));
        var input = Encoding.UTF8.GetBytes($"{userId}|{canonical}");
        return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
    }

    private async Task<(decimal TotalAmount, long AmountMinor)> PriceCartAsync(
        IReadOnlyCollection<PaymentCartItem> normalizedItems,
        CancellationToken cancellationToken)
    {
        var productIds = normalizedItems.Select(item => item.ProductId).ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.ProductId))
            .ToDictionaryAsync(product => product.ProductId, cancellationToken);

        decimal total = 0;
        foreach (var item in normalizedItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product) ||
                !product.IsActive ||
                product.IsDeleted)
            {
                throw new PaymentValidationException(
                    $"Product {item.ProductId} is unavailable.");
            }

            if (product.Stock < item.Quantity)
            {
                throw new PaymentValidationException(
                    $"Insufficient stock for {product.Name}.");
            }

            total = checked(total + product.Price * item.Quantity);
        }

        if (total <= 0)
        {
            throw new PaymentValidationException("The server-calculated total is invalid.");
        }

        return (total, ToMinorUnits(total));
    }

    private static long ToMinorUnits(decimal amount)
    {
        return checked((long)decimal.Round(
            amount * 100m,
            0,
            MidpointRounding.AwayFromZero));
    }

    private static string NormalizeCurrency(string? currency)
    {
        var normalized = string.IsNullOrWhiteSpace(currency)
            ? "usd"
            : currency.Trim().ToLowerInvariant();

        if (normalized.Length != 3 || normalized.Any(character => character is < 'a' or > 'z'))
        {
            throw new InvalidOperationException("Stripe:Currency must be a three-letter ISO currency code.");
        }

        return normalized;
    }

    private static string NormalizeCheckoutReference(string? checkoutReference)
    {
        return Guid.TryParse(checkoutReference, out var parsed)
            ? parsed.ToString("N")
            : Guid.NewGuid().ToString("N");
    }

    private static bool FixedTimeEquals(string? left, string? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));
    }
}
