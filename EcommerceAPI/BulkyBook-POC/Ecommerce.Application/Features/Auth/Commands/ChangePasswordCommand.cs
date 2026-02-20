using MediatR;

namespace Ecommerce.Application.Features.Auth.Commands
{
    public class ChangePasswordCommand : IRequest<bool>
    {
        public long UserId { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
