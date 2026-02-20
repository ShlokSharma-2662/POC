using MediatR;

namespace Ecommerce.Application.Features.Products.Commands
{
    // Soft delete
    public class DeleteProductCommand : IRequest<bool>
    {
        public int ProductId { get; set; }
        public bool Restore { get; set; } = false; // if true, undo delete
    }
}



