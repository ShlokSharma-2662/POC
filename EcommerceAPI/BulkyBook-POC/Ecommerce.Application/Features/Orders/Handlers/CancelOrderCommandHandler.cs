using Ecommerce.Application.Features.Orders.Commands;
using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Features.Orders.Handlers
{
    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
    {
        private readonly AppDbContext _context;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly IMessagePublisherService _messagePublisherService;

        public CancelOrderCommandHandler(
            AppDbContext context,
            ICacheInvalidationService cacheInvalidationService,
            IMessagePublisherService messagePublisherService)
        {
            _context = context;
            _cacheInvalidationService = cacheInvalidationService;
            _messagePublisherService = messagePublisherService;
        }

        public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(item => item.Id == request.OrderId, cancellationToken);
            if (order == null)
            {
                return false;
            }

            if (!OrderStatuses.TryNormalize(order.Status, out var currentStatus))
            {
                currentStatus = OrderStatuses.Pending;
            }

            if (!OrderStatuses.CanTransition(currentStatus, OrderStatuses.Cancelled))
            {
                throw new InvalidOperationException($"Order in '{currentStatus}' state cannot be cancelled.");
            }

            order.Status = OrderStatuses.Cancelled;
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
                OrderStatuses.Cancelled,
                DateTime.UtcNow,
                $"ORD-{order.Id.ToString("N")[..8].ToUpperInvariant()}");

            return true;
        }
    }
}
