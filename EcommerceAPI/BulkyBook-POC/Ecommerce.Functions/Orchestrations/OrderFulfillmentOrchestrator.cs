using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Functions.Orchestrations;

/// <summary>
/// Durable Function orchestrator managing the Order fulfillment state machine:
/// Pending -> StockChecked -> PaymentProcessed -> Confirmed -> Shipped -> Delivered
/// </summary>
public static class OrderFulfillmentOrchestrator
{
    [Function(nameof(RunOrderFulfillment))]
    public static async Task<OrderFulfillmentResult> RunOrderFulfillment(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(nameof(OrderFulfillmentOrchestrator));
        var input = context.GetInput<OrderFulfillmentInput>()
            ?? throw new ArgumentNullException(nameof(context), "OrderFulfillmentInput is required.");

        logger.LogInformation("Starting order fulfillment for OrderId: {OrderId}", input.OrderId);

        var result = new OrderFulfillmentResult
        {
            OrderId = input.OrderId,
            Status = "Pending"
        };

        try
        {
            // Step 1: Check stock availability for all items (fan-out/fan-in)
            var stockCheckTasks = new List<Task<StockCheckResult>>();
            foreach (var item in input.Items)
            {
                stockCheckTasks.Add(context.CallActivityAsync<StockCheckResult>(
                    nameof(Activities.CheckStockActivity.CheckStock),
                    new StockCheckInput { ProductId = item.ProductId, Quantity = item.Quantity }));
            }

            var stockResults = await Task.WhenAll(stockCheckTasks);

            if (stockResults.Any(r => !r.IsAvailable))
            {
                var unavailable = stockResults.Where(r => !r.IsAvailable).Select(r => r.ProductId);
                result.Status = "StockUnavailable";
                result.Message = $"Insufficient stock for products: {string.Join(", ", unavailable)}";
                logger.LogWarning("Stock check failed for order {OrderId}: {Message}", input.OrderId, result.Message);
                return result;
            }

            result.Status = "StockChecked";
            logger.LogInformation("Stock check passed for order {OrderId}", input.OrderId);

            // Step 2: Process payment
            var paymentResult = await context.CallActivityAsync<PaymentResult>(
                nameof(Activities.ProcessPaymentActivity.ProcessPayment),
                new PaymentInput
                {
                    OrderId = input.OrderId,
                    TotalAmount = input.TotalAmount,
                    UserId = input.UserId
                });

            if (!paymentResult.IsSuccessful)
            {
                result.Status = "PaymentFailed";
                result.Message = $"Payment failed: {paymentResult.ErrorMessage}";
                logger.LogWarning("Payment failed for order {OrderId}: {Message}", input.OrderId, result.Message);
                return result;
            }

            result.Status = "Confirmed";
            result.PaymentTransactionId = paymentResult.TransactionId;
            logger.LogInformation("Order {OrderId} confirmed with payment {TxnId}", input.OrderId, paymentResult.TransactionId);

            // Step 3: Wait for shipping confirmation (external event or timer)
            using var shippingCts = new CancellationTokenSource();
            var shippingDeadline = context.CurrentUtcDateTime.AddHours(24);

            var shippingEvent = context.WaitForExternalEvent<string>("ShipmentDispatched");
            var timeout = context.CreateTimer(shippingDeadline, shippingCts.Token);

            var winner = await Task.WhenAny(shippingEvent, timeout);
            if (winner == shippingEvent)
            {
                shippingCts.Cancel();
                result.Status = "Shipped";
                result.TrackingNumber = await shippingEvent;
                logger.LogInformation("Order {OrderId} shipped with tracking: {Tracking}", input.OrderId, result.TrackingNumber);
            }
            else
            {
                result.Status = "ShippingPending";
                result.Message = "Shipping not confirmed within 24 hours. Awaiting manual follow-up.";
                logger.LogWarning("Order {OrderId} shipping timed out.", input.OrderId);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Order fulfillment failed for OrderId: {OrderId}", input.OrderId);
            result.Status = "Failed";
            result.Message = ex.Message;
            return result;
        }
    }
}

#region Models

public class OrderFulfillmentInput
{
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItemInput> Items { get; set; } = new();
}

public class OrderItemInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class OrderFulfillmentResult
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? TrackingNumber { get; set; }
}

public class StockCheckInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class StockCheckResult
{
    public int ProductId { get; set; }
    public bool IsAvailable { get; set; }
    public int AvailableQuantity { get; set; }
}

public class PaymentInput
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class PaymentResult
{
    public bool IsSuccessful { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
