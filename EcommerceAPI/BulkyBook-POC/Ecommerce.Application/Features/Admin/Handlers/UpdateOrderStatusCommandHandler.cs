using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheInvalidationService _cacheInvalidationService;

        public UpdateOrderStatusCommandHandler(AppDbContext context, ICacheInvalidationService cacheInvalidationService)
        {
            _context = context;
            _cacheInvalidationService = cacheInvalidationService;
        }

        public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
            if (order == null) return false;

            order.Status = request.Status;
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate order cache after successful status update
            await _cacheInvalidationService.InvalidateOrderCacheAsync(request.OrderId);

            return true;
        }
    }
}