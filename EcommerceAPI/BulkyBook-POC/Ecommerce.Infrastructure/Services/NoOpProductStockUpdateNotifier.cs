using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// No-operation implementation used by services that do not expose stock realtime channels.
    /// </summary>
    public class NoOpProductStockUpdateNotifier : IProductStockUpdateNotifier
    {
        private readonly ILogger<NoOpProductStockUpdateNotifier> _logger;

        public NoOpProductStockUpdateNotifier(ILogger<NoOpProductStockUpdateNotifier> logger)
        {
            _logger = logger;
        }

        public Task NotifyProductStockUpdatedAsync(
            int productId,
            string productName,
            int stock,
            string changeType,
            string? stockStatus = null,
            DateTime? changedAt = null)
        {
            _logger.LogInformation(
                "Stock update realtime notification skipped. ProductId={ProductId}, Name={ProductName}, Stock={Stock}, Status={StockStatus}, ChangeType={ChangeType}, ChangedAt={ChangedAt}",
                productId,
                productName,
                stock,
                stockStatus,
                changeType,
                changedAt);

            return Task.CompletedTask;
        }
    }
}
