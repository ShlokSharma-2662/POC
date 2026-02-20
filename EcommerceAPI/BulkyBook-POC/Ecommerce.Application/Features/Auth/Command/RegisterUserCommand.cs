using Ecommerce.Application.Features.Auth.Models;
using MediatR;

namespace Ecommerce.Application.Features.Auth.Command
{
    public class RegisterUserCommand : IRequest<AuthResult>
    {
        public long Id { get; set; } = 0;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Role { get; set; } = "User";
    }
}
