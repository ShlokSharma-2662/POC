using MediatR;

namespace Ecommerce.Application.Features.Cart.Commands
{
    public class DeleteCartItemCommand : IRequest<Unit>
    {
        public int ProductId { get; set; }
    }
}


