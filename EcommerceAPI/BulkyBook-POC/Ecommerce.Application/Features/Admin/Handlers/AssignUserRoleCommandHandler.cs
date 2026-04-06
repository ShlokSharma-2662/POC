using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheInvalidationService _cacheInvalidationService;

        public AssignUserRoleCommandHandler(AppDbContext context, ICacheInvalidationService cacheInvalidationService)
        {
            _context = context;
            _cacheInvalidationService = cacheInvalidationService;
        }

        public async Task<bool> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user == null) return false;

            var validRoles = new[] { "Admin", "User", "Customer", "WarehouseManager" };
            if (!validRoles.Contains(request.Role))
                throw new ArgumentException("Invalid role.");

            user.Role = request.Role;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            
            // Invalidate user cache after successful update
            await _cacheInvalidationService.InvalidateUserCacheAsync();
            
            return true;
        }
    }
}


