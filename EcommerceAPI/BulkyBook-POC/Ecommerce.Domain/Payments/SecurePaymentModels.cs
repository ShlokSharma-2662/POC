namespace Ecommerce.Domain.Payments;

public sealed record PaymentCartItem(int ProductId, int Quantity);

public sealed record PaymentIntentCreation(
    long AmountMinor,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata,
    string IdempotencyKey);

public sealed record PaymentIntentSnapshot(
    string Id,
    string ClientSecret,
    long AmountMinor,
    string Currency,
    string Status,
    IReadOnlyDictionary<string, string> Metadata,
    bool IsRefunded);

public sealed record SecurePaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    long AmountMinor,
    string Currency,
    string CartFingerprint);

public sealed record VerifiedCheckoutPayment(
    string PaymentIntentId,
    long AmountMinor,
    decimal TotalAmount,
    string Currency,
    string CartFingerprint,
    Guid? ExistingOrderId);

public sealed class PaymentValidationException : Exception
{
    public PaymentValidationException(string message) : base(message)
    {
    }
}

public sealed class PaymentRejectedException : Exception
{
    public PaymentRejectedException(string message) : base(message)
    {
    }
}
