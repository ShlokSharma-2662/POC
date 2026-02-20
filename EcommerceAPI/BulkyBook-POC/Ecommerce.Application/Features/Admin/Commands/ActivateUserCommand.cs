using MediatR;

namespace Ecommerce.Application.Features.Admin.Commands
{
    public class ActivateUserCommand : IRequest<bool>
    {
        public long UserId { get; set; }
    }
}

