using MediatR;

namespace Ecommerce.Application.Features.Admin.Commands
{
    public class AssignUserRoleCommand : IRequest<bool>
    {
        public long UserId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}


