using MediatR;

namespace Ecommerce.Application.Features.Wishlist.Commands
{
    public class AddToWishlistCommand : IRequest<bool>
    {
        public int ProductId { get; set; }
        public long UserId { get; set; }
    }
}
