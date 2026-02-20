using Ecommerce.Application.Features.Admin.Models;
using MediatR;

namespace Ecommerce.Application.Features.Admin.Queries
{
    public class GetUserByIdQuery : IRequest<AdminUserDto?>
    {
        public long UserId { get; set; }
    }
}


