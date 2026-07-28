using MediatR;
using System.Collections.Generic;

namespace Ecommerce.Application.Features.Orders.Commands
{
    public class CheckoutOrderCommand : IRequest<Guid>
    {
        public string FullName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string PaymentIntentId { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();

        [System.Text.Json.Serialization.JsonIgnore]
        public string VerifiedCartFingerprint { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public decimal VerifiedTotalAmount { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string VerifiedCurrency { get; set; } = string.Empty;
    }
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
