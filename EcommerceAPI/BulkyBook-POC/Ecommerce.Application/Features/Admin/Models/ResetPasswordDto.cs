namespace Ecommerce.Application.Features.Admin.Models
{
    public class ResetPasswordDto 
    { 
        public long UserId { get; set; } 
        public string NewPassword { get; set; } = string.Empty; 
    }
}
