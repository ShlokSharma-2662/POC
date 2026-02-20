using MediatR;

namespace Ecommerce.Application.Features.Wishlist.Queries
{
    public class CheckWishlistStatusQuery : IRequest<bool>
    {
        public int ProductId { get; set; }
        public long UserId { get; set; }
    }
}
