using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheInvalidationService _cacheInvalidationService;

        public ResetUserPasswordCommandHandler(AppDbContext context, ICacheInvalidationService cacheInvalidationService)
        {
            _context = context;
            _cacheInvalidationService = cacheInvalidationService;
        }

        public async Task<bool> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user == null) return false;

            // basic password strength validation
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters long.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidate user cache after successful update
            await _cacheInvalidationService.InvalidateUserCacheAsync();
            
            return true;
        }
    }
}


