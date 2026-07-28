using Ecommerce.Domain.Payments;

namespace Ecommerce.Domain.Interfaces;

public interface ISecureCheckoutPaymentService
{
    Task<SecurePaymentIntentResult> CreatePaymentIntentAsync(
        long userId,
        IReadOnlyCollection<PaymentCartItem> items,
        string? checkoutReference,
        CancellationToken cancellationToken);

    Task<VerifiedCheckoutPayment> VerifyCheckoutPaymentAsync(
        long userId,
        string paymentIntentId,
        IReadOnlyCollection<PaymentCartItem> items,
        CancellationToken cancellationToken);

    Task RefundAfterOrderFailureAsync(
        string paymentIntentId,
        CancellationToken cancellationToken);
}
