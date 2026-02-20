using MediatR;
using System.Collections.Generic;

namespace Ecommerce.Application.Features.Orders.Commands
{
    public class CheckoutOrderCommand : IRequest<Guid>
    {
        public string FullName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public List<OrderItemDto> Items { get; set; } = new();
    }
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
