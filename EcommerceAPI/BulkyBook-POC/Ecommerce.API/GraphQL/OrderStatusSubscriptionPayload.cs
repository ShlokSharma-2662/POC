namespace Ecommerce.API.GraphQL
{
    public class OrderStatusSubscriptionPayload
    {
        public Guid OrderId { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }
}
