using MediatR;

namespace Ecommerce.Application.Features.Orders.Commands
{
    public class CancelOrderCommand : IRequest<bool>
    {
        public Guid OrderId { get; set; }
    }
}
