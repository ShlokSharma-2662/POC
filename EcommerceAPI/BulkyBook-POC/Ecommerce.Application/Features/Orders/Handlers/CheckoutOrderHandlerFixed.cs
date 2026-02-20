using Ecommerce.Application.Features.Orders.Commands;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Transactions;
using static Ecommerce.Application.Features.Orders.Commands.CheckoutOrderCommand;

namespace Ecommerce.Application.Features.Orders.Handlers
{
    /// <summary>
    /// Fixed CheckoutOrderHandler with proper concurrency control and race condition prevention
    /// </summary>
    public class CheckoutOrderHandlerFixed : IRequestHandler<CheckoutOrderCommand, Guid>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ILogger<CheckoutOrderHandlerFixed> _logger;

        public CheckoutOrderHandlerFixed(
            AppDbContext context, 
            IHttpContextAccessor httpContextAccessor, 
            IEmailService emailService, 
            ICacheInvalidationService cacheInvalidationService, 
            ILogger<CheckoutOrderHandlerFixed> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _cacheInvalidationService = cacheInvalidationService;
            _logger = logger;
        }

        public async Task<Guid> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Use a distributed transaction to ensure atomicity
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            try
            {
                // Step 1: Validate and reserve stock atomically
                await ValidateAndReserveStockAsync(request.Items, cancellationToken);

                // Step 2: Create order
                var order = await CreateOrderAsync(request, userId, cancellationToken);

                // Step 3: Update stock (already reserved, so this is safe)
                await UpdateStockAsync(request.Items, cancellationToken);

                // Step 4: Commit transaction
                transaction.Complete();

                // Step 5: Send confirmation email (outside transaction)
                await SendOrderConfirmationEmailAsync(order, request, cancellationToken);

                // Step 6: Invalidate caches
                await _cacheInvalidationService.InvalidateAllOrdersCacheAsync();

                _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.Id, userId);
                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Validates stock availability and reserves it atomically using database constraints
        /// </summary>
        private async Task ValidateAndReserveStockAsync(List<OrderItemDto> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                // Use raw SQL with optimistic concurrency control
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE Products 
                      SET Stock = Stock - {0} 
                      WHERE ProductId = {1} 
                        AND Stock >= {0} 
                        AND IsDeleted = 0",
                    item.Quantity, item.ProductId);

                if (rowsAffected == 0)
                {
                    // Get current stock for better error message
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductId == item.ProductId, cancellationToken);
                    
                    var availableStock = product?.Stock ?? 0;
                    var productName = product?.Name ?? "Unknown Product";
                    
                    throw new InvalidOperationException(
                        $"Insufficient stock for {productName}. Available: {availableStock}, Requested: {item.Quantity}. " +
                        "Another customer may have just purchased this item. Please refresh and try again.");
                }

                _logger.LogInformation("Reserved {Quantity} units of product {ProductId}", item.Quantity, item.ProductId);
            }
        }

        /// <summary>
        /// Creates the order after stock is reserved
        /// </summary>
        private async Task<Order> CreateOrderAsync(CheckoutOrderCommand request, string userId, CancellationToken cancellationToken)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.FullName,
                ShippingAddress = request.Address,
                Phone = request.PhoneNumber,
                UserId = Convert.ToInt64(userId!),
                CreatedAt = DateTime.Now,
                Items = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);
            
            return order;
        }

        /// <summary>
        /// Updates stock (stock was already decremented in ValidateAndReserveStockAsync)
        /// This method is kept for cache invalidation
        /// </summary>
        private async Task UpdateStockAsync(List<OrderItemDto> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                // Invalidate product caches to reflect updated stock
                await _cacheInvalidationService.InvalidateProductCacheAsync(item.ProductId);
            }
        }

        private async Task SendOrderConfirmationEmailAsync(Order order, CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            // Get user information
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == order.UserId, cancellationToken);
            if (user == null) return;

            try
            {
                // Get product details for email
                var orderItemsForEmail = new List<OrderItemEmailDto>();
                decimal totalAmount = 0;

                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId, cancellationToken);
                    if (product != null)
                    {
                        var itemTotal = product.Price * item.Quantity;
                        totalAmount += itemTotal;

                        orderItemsForEmail.Add(new OrderItemEmailDto
                        {
                            ProductName = product.Name,
                            Quantity = item.Quantity,
                            Price = product.Price,
                            TotalPrice = itemTotal,
                            ImageUrl = product.ImageUrl
                        });
                    }
                }

                // Send email
                await _emailService.SendOrderConfirmationEmailAsync(
                    user.Email,
                    user.FirstName,
                    order.Id.ToString(),
                    orderItemsForEmail,
                    totalAmount,
                    order.ShippingAddress,
                    order.Phone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}", order.Id);
                // Don't throw - email failure shouldn't fail the order
            }
        }
    }
}
