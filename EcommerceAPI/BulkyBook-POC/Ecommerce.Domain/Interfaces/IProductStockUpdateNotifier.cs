using System;

namespace Ecommerce.Domain.Interfaces
{
    public interface IProductStockUpdateNotifier
    {
        Task NotifyProductStockUpdatedAsync(
            int productId,
            string productName,
            int stock,
            string changeType,
            string? stockStatus = null,
            DateTime? changedAt = null);
    }
}
