using Ecommerce.Domain.Payments;

namespace Ecommerce.Domain.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentIntentSnapshot> CreatePaymentIntentAsync(
        PaymentIntentCreation request,
        CancellationToken cancellationToken);

    Task<PaymentIntentSnapshot> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken);

    Task RefundPaymentIntentAsync(
        string paymentIntentId,
        string reason,
        CancellationToken cancellationToken);
}
