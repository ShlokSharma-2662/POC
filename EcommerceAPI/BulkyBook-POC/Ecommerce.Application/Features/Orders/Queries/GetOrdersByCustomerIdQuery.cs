using Ecommerce.Application.Features.Orders.Models;
using MediatR;

namespace Ecommerce.Application.Features.Orders.Queries
{
    public class GetOrdersByCustomerIdQuery : IRequest<List<OrderDto>>
    {
        public long CustomerId { get; set; }
    }
}
