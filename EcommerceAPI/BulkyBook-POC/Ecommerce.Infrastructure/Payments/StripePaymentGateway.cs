using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain.Payments;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Ecommerce.Infrastructure.Payments;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly string _secretKey;

    public StripePaymentGateway(IConfiguration configuration)
    {
        _secretKey = configuration["Stripe:SecretKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_secretKey))
        {
            throw new InvalidOperationException("Stripe:SecretKey is not configured.");
        }
    }

    public async Task<PaymentIntentSnapshot> CreatePaymentIntentAsync(
        PaymentIntentCreation request,
        CancellationToken cancellationToken)
    {
        var service = new PaymentIntentService(new StripeClient(_secretKey));
        var intent = await service.CreateAsync(
            new PaymentIntentCreateOptions
            {
                Amount = request.AmountMinor,
                Currency = request.Currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = new Dictionary<string, string>(request.Metadata)
            },
            new RequestOptions { IdempotencyKey = request.IdempotencyKey },
            cancellationToken);

        return await ToSnapshotAsync(intent, cancellationToken);
    }

    public async Task<PaymentIntentSnapshot> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        var service = new PaymentIntentService(new StripeClient(_secretKey));
        var intent = await service.GetAsync(
            paymentIntentId,
            options: null,
            requestOptions: null,
            cancellationToken);

        return await ToSnapshotAsync(intent, cancellationToken);
    }

    public async Task RefundPaymentIntentAsync(
        string paymentIntentId,
        string reason,
        CancellationToken cancellationToken)
    {
        var service = new RefundService(new StripeClient(_secretKey));
        await service.CreateAsync(
            new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Reason = RefundReasons.RequestedByCustomer,
                Metadata = new Dictionary<string, string>
                {
                    ["checkout_compensation_reason"] = reason
                }
            },
            new RequestOptions
            {
                IdempotencyKey = $"order-compensation:{paymentIntentId}"
            },
            cancellationToken);
    }

    private async Task<PaymentIntentSnapshot> ToSnapshotAsync(
        PaymentIntent intent,
        CancellationToken cancellationToken)
    {
        var refundService = new RefundService(new StripeClient(_secretKey));
        var refunds = await refundService.ListAsync(
            new RefundListOptions
            {
                PaymentIntent = intent.Id,
                Limit = 1
            },
            requestOptions: null,
            cancellationToken);

        var isRefunded = refunds.Data.Any(refund =>
            string.Equals(refund.Status, "succeeded", StringComparison.OrdinalIgnoreCase));

        return new PaymentIntentSnapshot(
            intent.Id,
            intent.ClientSecret ?? string.Empty,
            intent.Amount,
            intent.Currency,
            intent.Status,
            new Dictionary<string, string>(intent.Metadata),
            isRefunded);
    }
}
