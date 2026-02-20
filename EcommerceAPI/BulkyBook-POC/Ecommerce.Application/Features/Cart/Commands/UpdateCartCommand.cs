using MediatR;

namespace Ecommerce.Application.Features.Cart.Commands
{
    public class UpdateCartCommand : IRequest<Unit>
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}


