namespace Ecommerce.Application.Features.Orders.Models
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public long UserId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string ShippingAddress { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        
        public string Status { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }
    public static class OrderStatuses
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Confirmed, Shipped, Delivered, Cancelled };

        public static bool TryNormalize(string? status, out string normalizedStatus)
        {
            normalizedStatus = status?.Trim().ToLowerInvariant() switch
            {
                "pending" => Pending,
                "confirmed" => Confirmed,
                "shipped" => Shipped,
                "delivered" => Delivered,
                "cancelled" => Cancelled,
                _ => string.Empty
            };

            return !string.IsNullOrWhiteSpace(normalizedStatus);
        }

        public static bool CanTransition(string currentStatus, string newStatus)
        {
            if (!TryNormalize(currentStatus, out var normalizedCurrent) || !TryNormalize(newStatus, out var normalizedNext))
            {
                return false;
            }

            if (normalizedCurrent == normalizedNext)
            {
                return true;
            }

            return normalizedCurrent switch
            {
                Pending => normalizedNext is Confirmed or Cancelled,
                Confirmed => normalizedNext is Shipped or Cancelled,
                Shipped => normalizedNext == Delivered,
                Delivered => false,
                Cancelled => false,
                _ => false
            };
        }
    }
}
