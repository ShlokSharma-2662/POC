using Ecommerce.Application.Features.Cart.Commands;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ecommerce.Application.Features.Cart.Handlers
{
    public class DeleteCartItemCommandHandler : IRequestHandler<DeleteCartItemCommand, Unit>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheInvalidationService _cacheInvalidationService;

        public DeleteCartItemCommandHandler(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ICacheInvalidationService cacheInvalidationService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _cacheInvalidationService = cacheInvalidationService;
        }

        public async Task<Unit> Handle(DeleteCartItemCommand request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                         ?? throw new UnauthorizedAccessException();

            var cart = await _context.Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (item != null)
                {
                    _context.CartItems.Remove(item);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            if (long.TryParse(userId, out var numericUserId))
            {
                await _cacheInvalidationService.InvalidateCartCacheAsync(numericUserId);
            }

            return Unit.Value;
        }
    }
}


