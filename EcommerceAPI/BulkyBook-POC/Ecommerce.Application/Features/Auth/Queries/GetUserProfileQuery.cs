using Ecommerce.Application.Features.Auth.Models;
using MediatR;

namespace Ecommerce.Application.Features.Auth.Queries
{
    public class GetUserProfileQuery : IRequest<UserProfileDto>
    {
        public long UserId { get; set; }
    }
}
