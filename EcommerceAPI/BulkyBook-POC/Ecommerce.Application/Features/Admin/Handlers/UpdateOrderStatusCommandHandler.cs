using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Admin.Handlers
{
    public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly IMessagePublisherService _messagePublisherService;

        public UpdateOrderStatusCommandHandler(
            AppDbContext context,
            ICacheInvalidationService cacheInvalidationService,
            IMessagePublisherService messagePublisherService)
        {
            _context = context;
            _cacheInvalidationService = cacheInvalidationService;
            _messagePublisherService = messagePublisherService;
        }

        public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
        {
            if (request.OrderId == Guid.Empty)
            {
                return false;
            }

            var order = await _context.Orders.FirstOrDefaultAsync(item => item.Id == request.OrderId, cancellationToken);
            if (order == null)
            {
                return false;
            }

            if (!OrderStatuses.TryNormalize(order.Status, out var currentStatus))
            {
                currentStatus = OrderStatuses.Pending;
            }

            if (!OrderStatuses.TryNormalize(request.Status, out var newStatus))
            {
                throw new ArgumentException($"Invalid order status: '{request.Status}'");
            }

            if (!OrderStatuses.CanTransition(currentStatus, newStatus))
            {
                throw new InvalidOperationException($"Order in '{currentStatus}' state cannot transition to '{newStatus}'.");
            }

            if (currentStatus == newStatus)
            {
                return true;
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync(cancellationToken);

            await _cacheInvalidationService.InvalidateOrderCacheAsync(request.OrderId);

            var user = await _context.Users
                .AsNoTracking()
                .Where(item => item.Id == order.UserId)
                .Select(item => new { item.Id, item.Email })
                .FirstOrDefaultAsync(cancellationToken);

            await _messagePublisherService.PublishOrderStatusChangedAsync(
                order.Id,
                user?.Id.ToString() ?? order.UserId.ToString(),
                user?.Email ?? "unknown@example.com",
                currentStatus,
                newStatus,
                DateTime.UtcNow,
                $"ORD-{order.Id.ToString("N")[..8].ToUpperInvariant()}");

            return true;
        }
    }
}
