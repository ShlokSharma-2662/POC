using Ecommerce.Application.Features.Products.Models;
using MediatR;

namespace Ecommerce.Application.Features.Products.Queries
{
    public class GetProductByIdQuery : IRequest<ProductViewModel?>
    {
        public int ProductId { get; set; }
    }
}
