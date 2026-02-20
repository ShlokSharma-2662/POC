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
using static Ecommerce.Application.Features.Orders.Commands.CheckoutOrderCommand;

namespace Ecommerce.Application.Features.Orders.Handlers
{
    public class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, Guid>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ILogger<CheckoutOrderHandler> _logger;

        public CheckoutOrderHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor, IEmailService emailService, ICacheInvalidationService cacheInvalidationService, ILogger<CheckoutOrderHandler> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _cacheInvalidationService = cacheInvalidationService;
            _logger = logger;
        }

        public async Task<Guid> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User ID not found in claims. Please ensure you are authenticated.");
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.FullName,
                ShippingAddress = request.Address,
                Phone = request.PhoneNumber,
                UserId = Convert.ToInt64(userId),
                CreatedAt = DateTime.Now,
                Items = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var item in request.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId, cancellationToken);
                if (product == null)
                    throw new InvalidOperationException($"Product with ID {item.ProductId} not found. Please refresh the page and try again.");

                if (product.Stock < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}. Available: {product.Stock}, Requested: {item.Quantity}. Please adjust your order and try again.");

                product.Stock -= item.Quantity; // ✅ Decrease stock

                // Save immediately after decrementing stock to persist the change
                _context.Products.Update(product);

                // Invalidate product caches so storefront reflects updated stock immediately
                await _cacheInvalidationService.InvalidateProductCacheAsync(product.ProductId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Send order confirmation email
            await SendOrderConfirmationEmailAsync(order, request, cancellationToken);

            // Invalidate order cache after successful order creation
            await _cacheInvalidationService.InvalidateAllOrdersCacheAsync();

            return order.Id;
        }

        private async Task SendOrderConfirmationEmailAsync(Order order, CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            // Get user information
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == order.UserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User not found for order {OrderId} with UserId {UserId}. Email will not be sent.", order.Id, order.UserId);
                return;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("User {UserId} has no email address configured. Email will not be sent for order {OrderId}.", user.Id, order.Id);
                return;
            }

            _logger.LogInformation("Preparing to send order confirmation email for order {OrderId} to user {UserEmail} ({UserName})", 
                order.Id, user.Email, $"{user.FirstName} {user.LastName}");

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

                _logger.LogInformation("Sending order confirmation email for order {OrderId} to {UserEmail} with {ItemCount} items, total amount: {TotalAmount}", 
                    order.Id, user.Email, orderItemsForEmail.Count, totalAmount);

                // Send email
                await _emailService.SendOrderConfirmationEmailAsync(
                    user.Email,
                    user.FirstName + " " + user.LastName,
                    order.Id.ToString(),
                    orderItemsForEmail,
                    totalAmount,
                    request.Address,
                    request.PhoneNumber
                );

                _logger.LogInformation("Order confirmation email sent successfully for order {OrderId} to {UserEmail}", order.Id, user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation email for order {OrderId} to user {UserEmail} ({UserName}). " +
                    "Exception Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}. Order processing will continue.", 
                    order.Id, user.Email, $"{user.FirstName} {user.LastName}", 
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                
                // Log inner exception if present
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception when sending email for order {OrderId}: {InnerMessage}", 
                        order.Id, ex.InnerException.Message);
                }
            }
        }

    }

}
