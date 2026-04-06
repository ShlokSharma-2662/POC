using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Functions.Activities;

/// <summary>
/// Durable Function activity that processes payment for an order.
/// In production, this would integrate with Stripe or another payment gateway.
/// </summary>
public static class ProcessPaymentActivity
{
    [Function(nameof(ProcessPayment))]
    public static async Task<Orchestrations.PaymentResult> ProcessPayment(
        [ActivityTrigger] Orchestrations.PaymentInput input,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(ProcessPaymentActivity));
        logger.LogInformation(
            "Processing payment for OrderId: {OrderId}, Amount: {Amount}, UserId: {UserId}",
            input.OrderId, input.TotalAmount, input.UserId);

        // TODO: Replace with actual Stripe API call
        // Simulating payment processing
        await Task.Delay(1000);

        var transactionId = $"TXN-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        logger.LogInformation(
            "Payment processed successfully for OrderId {OrderId}: TransactionId={TxnId}",
            input.OrderId, transactionId);

        return new Orchestrations.PaymentResult
        {
            IsSuccessful = true,
            TransactionId = transactionId
        };
    }
}
