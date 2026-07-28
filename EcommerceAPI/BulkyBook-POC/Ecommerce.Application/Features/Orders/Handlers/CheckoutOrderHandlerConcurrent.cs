using Ecommerce.Application.Features.Orders.Commands;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Persistence;
using Ecommerce.Infrastructure.Caching;
using Ecommerce.Infrastructure.Services;
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
    /// Production-ready CheckoutOrderHandler with comprehensive race condition prevention
    /// </summary>
    public class CheckoutOrderHandlerConcurrent
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly IMessagePublisherService _messagePublisher;
        private readonly ILogger<CheckoutOrderHandlerConcurrent> _logger;

        public CheckoutOrderHandlerConcurrent(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            ICacheInvalidationService cacheInvalidationService,
            IMessagePublisherService messagePublisher,
            ILogger<CheckoutOrderHandlerConcurrent> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _cacheInvalidationService = cacheInvalidationService;
            _messagePublisher = messagePublisher;
            _logger = logger;
        }

        public async Task<Guid> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@example.com";

            _logger.LogInformation("Starting order checkout for user {UserId} with {ItemCount} items", 
                userId, request.Items.Count);

            // Use distributed transaction for complete atomicity
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            try
            {
                // Step 1: Validate all products exist and get current stock
                var productValidations = await ValidateProductsAsync(request.Items, cancellationToken);

                // Step 2: Atomically reserve stock for all products
                var reservations = request.Items.Select(item => new ProductReservation 
                { 
                    ProductId = item.ProductId, 
                    Quantity = item.Quantity 
                }).ToList();

                var stockReserved = await _context.TryReserveStockBatchAsync(reservations, cancellationToken);
                
                if (!stockReserved)
                {
                    // Get detailed stock information for error message
                    var stockDetails = await GetStockDetailsAsync(request.Items, cancellationToken);
                    throw new InvalidOperationException(
                        $"Insufficient stock available. {string.Join(", ", stockDetails)}. " +
                        "Another customer may have just purchased these items. Please refresh and try again.");
                }

                _logger.LogInformation("Successfully reserved stock for {ItemCount} products", request.Items.Count);

                // Step 3: Create order
                var order = await CreateOrderAsync(request, userId, cancellationToken);

                // Step 4: Commit transaction
                transaction.Complete();

                _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.Id, userId);

                // Step 5: Publish order created event (outside transaction)
                await PublishOrderCreatedEventAsync(order, request, userId, userEmail);

                // Step 6: Invalidate caches (outside transaction)
                await InvalidateCachesAsync(request.Items);

                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order for user {UserId}. Error: {ErrorMessage}", 
                    userId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Validates that all products exist and are available
        /// </summary>
        private async Task<List<ProductValidation>> ValidateProductsAsync(
            List<OrderItemDto> items, 
            CancellationToken cancellationToken)
        {
            var productIds = items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId) && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            var validations = new List<ProductValidation>();

            foreach (var item in items)
            {
                var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                
                if (product == null)
                {
                    throw new InvalidOperationException($"Product with ID {item.ProductId} not found or has been removed.");
                }

                validations.Add(new ProductValidation
                {
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    RequestedQuantity = item.Quantity,
                    AvailableStock = product.Stock,
                    IsValid = product.Stock >= item.Quantity
                });
            }

            return validations;
        }

        /// <summary>
        /// Gets detailed stock information for error messages
        /// </summary>
        private async Task<List<string>> GetStockDetailsAsync(
            List<OrderItemDto> items, 
            CancellationToken cancellationToken)
        {
            var details = new List<string>();
            
            foreach (var item in items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductId, cancellationToken);
                
                if (product != null)
                {
                    details.Add($"{product.Name}: Available {product.Stock}, Requested {item.Quantity}");
                }
            }

            return details;
        }

        /// <summary>
        /// Creates the order after stock is reserved
        /// </summary>
        private async Task<Order> CreateOrderAsync(
            CheckoutOrderCommand request, 
            string userId, 
            CancellationToken cancellationToken)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.FullName,
                ShippingAddress = request.Address,
                Phone = request.PhoneNumber,
                UserId = Convert.ToInt64(userId!),
                CreatedAt = DateTime.UtcNow,
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
        /// Publishes order created event to RabbitMQ
        /// </summary>
        private async Task PublishOrderCreatedEventAsync(
            Order order, 
            CheckoutOrderCommand request, 
            string userId, 
            string userEmail)
        {
            try
            {
                var orderNumber = $"ORD-{order.Id.ToString().Substring(0, 8).ToUpper()}";
                // Calculate total amount from products
                decimal totalAmount = 0;
                foreach (var item in request.Items)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    if (product != null)
                    {
                        totalAmount += product.Price * item.Quantity;
                    }
                }

                await _messagePublisher.PublishOrderCreatedAsync(
                    order.Id,
                    userId,
                    userEmail,
                    totalAmount,
                    order.CreatedAt,
                    orderNumber);

                _logger.LogInformation("Published OrderCreated event for Order {OrderNumber}", orderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreated event for Order {OrderId}", order.Id);
                // Don't throw - message publishing failure shouldn't fail the order
            }
        }

        /// <summary>
        /// Invalidates relevant caches
        /// </summary>
        private async Task InvalidateCachesAsync(List<OrderItemDto> items)
        {
            try
            {
                foreach (var item in items)
                {
                    await _cacheInvalidationService.InvalidateProductCacheAsync(item.ProductId);
                }
                
                await _cacheInvalidationService.InvalidateAllOrdersCacheAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to invalidate caches after order creation");
                // Don't throw - cache invalidation failure shouldn't fail the order
            }
        }
    }

    /// <summary>
    /// Represents product validation result
    /// </summary>
    public class ProductValidation
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public int AvailableStock { get; set; }
        public bool IsValid { get; set; }
    }
}
