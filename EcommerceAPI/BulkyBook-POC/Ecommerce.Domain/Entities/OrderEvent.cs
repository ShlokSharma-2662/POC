namespace Ecommerce.Domain.Entities;

public class OrderEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Guid OrderId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
    public string? UserId { get; set; }
}

public static class OrderEventTypes
{
    public const string OrderPlaced = "OrderPlaced";
    public const string OrderConfirmed = "OrderConfirmed";
    public const string OrderShipped = "OrderShipped";
    public const string OrderDelivered = "OrderDelivered";
    public const string OrderCancelled = "OrderCancelled";
    public const string PaymentProcessed = "PaymentProcessed";
    public const string PaymentFailed = "PaymentFailed";
    public const string StockReserved = "StockReserved";
    public const string StockReleaseFailed = "StockReleaseFailed";
}
