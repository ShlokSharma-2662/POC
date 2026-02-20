using Ecommerce.Application.Features.Auth.Commands;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Auth.Handlers
{
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, bool>
    {
        private readonly AppDbContext _context;

        public ChangePasswordHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.OldPassword) || 
                string.IsNullOrWhiteSpace(request.NewPassword) || 
                string.IsNullOrWhiteSpace(request.ConfirmPassword))
                throw new ArgumentException("All password fields are required.");

            if (request.NewPassword != request.ConfirmPassword)
                throw new ArgumentException("New password and confirm password do not match.");

            if (request.NewPassword.Length < 8)
                throw new ArgumentException("New password must be at least 8 characters long.");

            if (request.OldPassword == request.NewPassword)
                throw new ArgumentException("New password must be different from the old password.");

            // Get user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user == null)
                throw new ArgumentException("User not found.");

            // Verify old password
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                throw new ArgumentException("Current password is incorrect.");

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
