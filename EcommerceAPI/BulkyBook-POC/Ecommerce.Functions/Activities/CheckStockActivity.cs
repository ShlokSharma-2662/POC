using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Functions.Activities;

/// <summary>
/// Durable Function activity that checks product stock availability.
/// In production, this would call the Product Service API or query the database directly.
/// </summary>
public static class CheckStockActivity
{
    [Function(nameof(CheckStock))]
    public static async Task<Orchestrations.StockCheckResult> CheckStock(
        [ActivityTrigger] Orchestrations.StockCheckInput input,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(CheckStockActivity));
        logger.LogInformation(
            "Checking stock for ProductId: {ProductId}, Requested: {Quantity}",
            input.ProductId, input.Quantity);

        // TODO: Replace with actual Product Service gRPC/HTTP call
        // Simulating stock check with a delay
        await Task.Delay(500);

        // Simulated: all products have 100 units in stock
        var availableQuantity = 100;
        var isAvailable = availableQuantity >= input.Quantity;

        logger.LogInformation(
            "Stock check result for ProductId {ProductId}: Available={Available}, Stock={Stock}",
            input.ProductId, isAvailable, availableQuantity);

        return new Orchestrations.StockCheckResult
        {
            ProductId = input.ProductId,
            IsAvailable = isAvailable,
            AvailableQuantity = availableQuantity
        };
    }
}
