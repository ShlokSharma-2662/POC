using MediatR;
using Ecommerce.Application.Features.Wishlist.Models;

namespace Ecommerce.Application.Features.Wishlist.Queries
{
    public class GetUserWishlistQuery : IRequest<List<WishlistItemDto>>
    {
        public long UserId { get; set; }
    }
}
