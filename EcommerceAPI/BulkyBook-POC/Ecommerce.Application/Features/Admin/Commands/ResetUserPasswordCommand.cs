using MediatR;

namespace Ecommerce.Application.Features.Admin.Commands
{
    public class ResetUserPasswordCommand : IRequest<bool>
    {
        public long UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }
}


