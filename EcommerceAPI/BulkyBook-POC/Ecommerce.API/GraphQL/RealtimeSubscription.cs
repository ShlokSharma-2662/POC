using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Ecommerce.API.GraphQL
{
    [Authorize]
    public class RealtimeSubscription
    {
        [Subscribe]
        [Topic(GraphQlEventTopics.OrderStatusChanged)]
        public OrderStatusSubscriptionPayload OnOrderStatusChanged([EventMessage] OrderStatusSubscriptionPayload payload)
        {
            return payload;
        }

        [Subscribe]
        [Topic(GraphQlEventTopics.ProductStockUpdated)]
        public ProductStockUpdatePayload OnProductStockUpdated([EventMessage] ProductStockUpdatePayload payload)
        {
            return payload;
        }
    }
}
