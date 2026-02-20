using Ecommerce.Application.Features.Auth.Models;
using MediatR;

namespace Ecommerce.Application.Features.Auth.Command
{
    public class LoginUserCommand : IRequest<AuthResult>
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
