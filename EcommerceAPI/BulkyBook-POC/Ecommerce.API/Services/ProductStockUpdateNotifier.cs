using Ecommerce.API.GraphQL;
using Ecommerce.Domain.Interfaces;
using HotChocolate.Subscriptions;

namespace Ecommerce.API.Services
{
    public class ProductStockUpdateNotifier : IProductStockUpdateNotifier
    {
        private readonly ITopicEventSender _topicEventSender;

        public ProductStockUpdateNotifier(ITopicEventSender topicEventSender)
        {
            _topicEventSender = topicEventSender;
        }

        public async Task NotifyProductStockUpdatedAsync(
            int productId,
            string productName,
            int stock,
            string changeType,
            string? stockStatus = null,
            DateTime? changedAt = null)
        {
            var payload = new ProductStockUpdatePayload
            {
                ProductId = productId,
                ProductName = productName,
                Stock = stock,
                ChangeType = changeType,
                StockStatus = string.IsNullOrWhiteSpace(stockStatus)
                    ? (stock > 0 ? "InStock" : "OutOfStock")
                    : stockStatus,
                ChangedAt = changedAt ?? DateTime.UtcNow
            };

            await _topicEventSender.SendAsync(GraphQlEventTopics.ProductStockUpdated, payload);
        }
    }
}
