using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Ecommerce.ProductService.GraphQL
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
    }
}
