using Ecommerce.Application.Features.Cart.Models;
using MediatR;

namespace Ecommerce.Application.Features.Cart.Queries
{
    public class GetMyCartQuery : IRequest<List<CartItemDto>>
    {
    }
}


