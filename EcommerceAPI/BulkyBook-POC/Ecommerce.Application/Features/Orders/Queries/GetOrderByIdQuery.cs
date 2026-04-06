using Ecommerce.Application.Features.Orders.Models;
using MediatR;

namespace Ecommerce.Application.Features.Orders.Queries
{
    public class GetOrderByIdQuery : IRequest<OrderDto?>
    {
        public Guid OrderId { get; set; }
    }
}
