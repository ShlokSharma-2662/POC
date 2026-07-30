using Ecommerce.Application.Features.Orders.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Functions.Orchestrations;

/// <summary>
/// Durable Function orchestrator managing the Order fulfillment state machine:
/// Pending -> Confirmed -> Shipped -> Delivered
/// </summary>
public static class OrderFulfillmentOrchestrator
{
    private const int ShippingConfirmationTimeoutHours = 24;
    private const int DeliveryConfirmationTimeoutHours = 24;
    private const string ShipmentDispatchedEvent = "ShipmentDispatched";
    private const string OrderDeliveredEvent = "OrderDelivered";

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
            Status = OrderStatuses.Pending
        };

        try
        {
            // Step 1: Check stock availability for all items (fan-out/fan-in)
            if (input.Items?.Count > 0)
            {
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

                logger.LogInformation("Stock check passed for order {OrderId}", input.OrderId);
            }
            else
            {
                logger.LogWarning(
                    "No line items were provided for OrderId: {OrderId}. Continuing fulfillment with payment/shipping steps.",
                    input.OrderId);
            }

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

            result.Status = OrderStatuses.Confirmed;
            result.PaymentTransactionId = paymentResult.TransactionId;
            logger.LogInformation("Order {OrderId} confirmed with payment {TxnId}", input.OrderId, paymentResult.TransactionId);

            // Step 3: Wait for shipping confirmation (external event or timer)
            using var shippingCts = new CancellationTokenSource();
            var shippingDeadline = context.CurrentUtcDateTime.AddHours(ShippingConfirmationTimeoutHours);

            var shippingEvent = context.WaitForExternalEvent<FulfillmentTrackingPayload>(ShipmentDispatchedEvent);
            var timeout = context.CreateTimer(shippingDeadline, shippingCts.Token);

            var winner = await Task.WhenAny(shippingEvent, timeout);
            if (winner == shippingEvent)
            {
                shippingCts.Cancel();
                var shipmentPayload = await shippingEvent;
                result.Status = OrderStatuses.Shipped;
                result.TrackingNumber = shipmentPayload?.TrackingNumber;
                result.Message = string.IsNullOrWhiteSpace(shipmentPayload?.Notes) ? null : shipmentPayload.Notes;
                logger.LogInformation(
                    "Order {OrderId} shipped with tracking: {Tracking}",
                    input.OrderId,
                    result.TrackingNumber);

                // Step 4: Wait for delivery confirmation (external event or timer)
                using var deliveryCts = new CancellationTokenSource();
                var deliveryDeadline = context.CurrentUtcDateTime.AddHours(DeliveryConfirmationTimeoutHours);
                var deliveryEvent = context.WaitForExternalEvent<FulfillmentTrackingPayload>(OrderDeliveredEvent);
                var deliveryTimeout = context.CreateTimer(deliveryDeadline, deliveryCts.Token);
                var deliveryWinner = await Task.WhenAny(deliveryEvent, deliveryTimeout);

                if (deliveryWinner == deliveryEvent)
                {
                    deliveryCts.Cancel();
                    var deliveryPayload = await deliveryEvent;
                    result.Status = OrderStatuses.Delivered;
                    if (!string.IsNullOrWhiteSpace(deliveryPayload?.Notes))
                    {
                        result.Message = deliveryPayload.Notes;
                    }
                    logger.LogInformation(
                        "Order {OrderId} delivered with tracking: {Tracking}",
                        input.OrderId,
                        deliveryPayload?.TrackingNumber ?? result.TrackingNumber);
                }
                else
                {
                    result.Status = OrderStatuses.Shipped;
                    result.Message = $"Delivery not confirmed within {DeliveryConfirmationTimeoutHours} hours. Manual follow-up required.";
                    logger.LogWarning("Order {OrderId} delivery timed out.", input.OrderId);
                }
            }
            else
            {
                result.Status = OrderStatuses.Confirmed;
                result.Message = $"Shipping not confirmed within {ShippingConfirmationTimeoutHours} hours. Manual follow-up required.";
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

public class FulfillmentTrackingPayload
{
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? Notes { get; set; }
    public DateTime? EventTimeUtc { get; set; }
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
