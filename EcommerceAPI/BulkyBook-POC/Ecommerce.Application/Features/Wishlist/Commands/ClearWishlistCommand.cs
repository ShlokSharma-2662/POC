using MediatR;

namespace Ecommerce.Application.Features.Wishlist.Commands
{
    public class ClearWishlistCommand : IRequest<bool>
    {
        public long UserId { get; set; }
    }
}
