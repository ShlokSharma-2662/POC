namespace Ecommerce.Application.Features.Admin.Models
{
    public class AssignRoleDto 
    { 
        public long UserId { get; set; } 
        public string Role { get; set; } = string.Empty; 
    }
}
