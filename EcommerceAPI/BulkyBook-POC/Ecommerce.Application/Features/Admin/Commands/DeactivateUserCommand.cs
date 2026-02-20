using MediatR;

namespace Ecommerce.Application.Features.Admin.Commands
{
    public class DeactivateUserCommand : IRequest<bool>
    {
        public long UserId { get; set; }
    }
}


