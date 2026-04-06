using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Functions.Triggers;

/// <summary>
/// Timer-triggered Azure Function that runs every 15 minutes to check for low-stock products
/// and generates replenishment alerts. Implements the PRD requirement for cloud-based task automation.
/// </summary>
public static class StockReplenishmentCheck
{
    private const int LowStockThreshold = 10;

    [Function(nameof(CheckLowStock))]
    public static async Task CheckLowStock(
        [TimerTrigger("0 */15 * * * *")] TimerInfo timerInfo,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(StockReplenishmentCheck));

        logger.LogInformation(
            "Low stock replenishment check started at: {UtcNow}. Last run: {LastRun}",
            DateTime.UtcNow,
            timerInfo.ScheduleStatus?.Last);

        // TODO: Replace with actual Product Service API call or direct DB query
        // Simulating low-stock product detection
        var lowStockProducts = new[]
        {
            new { ProductId = 1, Name = "Sample Product A", Stock = 5 },
            new { ProductId = 7, Name = "Sample Product B", Stock = 3 }
        };

        foreach (var product in lowStockProducts)
        {
            if (product.Stock < LowStockThreshold)
            {
                logger.LogWarning(
                    "⚠️ LOW STOCK ALERT: Product '{ProductName}' (ID: {ProductId}) has only {Stock} units remaining (threshold: {Threshold})",
                    product.Name, product.ProductId, product.Stock, LowStockThreshold);

                // TODO: Publish low-stock event to Event Grid for notification service
                // TODO: Optionally trigger automatic replenishment workflow
            }
        }

        logger.LogInformation(
            "Low stock check completed. Found {Count} product(s) below threshold of {Threshold}.",
            lowStockProducts.Length, LowStockThreshold);

        await Task.CompletedTask;
    }
}
