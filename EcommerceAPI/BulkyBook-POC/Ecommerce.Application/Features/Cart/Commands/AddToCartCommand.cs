using Ecommerce.Application.Features.Cart.Models;
using MediatR;

namespace Ecommerce.Application.Features.Cart.Commands
{
    public class AddToCartCommand : IRequest<AddToCartResult>
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

