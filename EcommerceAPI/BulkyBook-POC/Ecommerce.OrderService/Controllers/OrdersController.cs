using Ecommerce.OrderService.GraphQL;
using Ecommerce.Application.Common.Services;
using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Application.Features.Orders.Commands;
using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Application.Features.Orders.Queries;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Services;
using HotChocolate.Subscriptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.OrderService.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly IMessagePublisherService _messagePublisher;
        private readonly IEventGridPublisherService _eventGridPublisher;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserContextService _userContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrdersController> _logger;
 
        public OrdersController(
            IMediator mediator, 
            IMessagePublisherService messagePublisher, 
            IEventGridPublisherService eventGridPublisher,
            IHttpClientFactory httpClientFactory,
            IUserContextService userContext,
            IConfiguration configuration,
            ILogger<OrdersController> logger)
        {
            _mediator = mediator;
            _messagePublisher = messagePublisher;
            _eventGridPublisher = eventGridPublisher;
            _httpClientFactory = httpClientFactory;
            _userContext = userContext;
            _configuration = configuration;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutOrderCommand command)
        {
            var startTime = DateTime.UtcNow;
            var requestId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                _logger.LogInformation(
                    "[{RequestId}] Order checkout request received: FullName={FullName}, Items={ItemCount}",
                    requestId,
                    command?.FullName,
                    command?.Items?.Count ?? 0);

                if (command == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        IsSuccessful = false,
                        Status = "ValidationError",
                        StatusReason = "Invalid order data."
                    });
                }

                if (command.Items == null || !command.Items.Any())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        IsSuccessful = false,
                        Status = "ValidationError",
                        StatusReason = "Order must contain at least one item."
                    });
                }

                if (string.IsNullOrWhiteSpace(command.FullName) ||
                    string.IsNullOrWhiteSpace(command.Address) ||
                    string.IsNullOrWhiteSpace(command.PhoneNumber))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        IsSuccessful = false,
                        Status = "ValidationError",
                        StatusReason = "Full name, address, and phone number are required."
                    });
                }

                _logger.LogInformation("[{RequestId}] Sending order command to mediator...", requestId);
                var mediatorStartTime = DateTime.UtcNow;
                var orderId = await _mediator.Send(command);
                var mediatorDuration = DateTime.UtcNow - mediatorStartTime;
                _logger.LogInformation(
                    "[{RequestId}] Order created successfully with ID: {OrderId} in {Duration}ms",
                    requestId,
                    orderId,
                    mediatorDuration.TotalMilliseconds);

                try
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@example.com";
                    var orderNumber = $"ORD-{orderId.ToString()[..8].ToUpperInvariant()}";

                    await _messagePublisher.PublishOrderCreatedAsync(
                        orderId,
                        userId,
                        userEmail,
                        0m,
                        DateTime.UtcNow,
                        orderNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish OrderCreated event for order {OrderId}", orderId);
                }
 
                // Publish OrderPlaced event to Event Grid for downstream consumption
                try
                {
                    var userId = _userContext.GetCurrentUserId();
                    await _eventGridPublisher.PublishOrderEventAsync(orderId, userId?.ToString() ?? "Anonymous", "OrderPlaced", new { ItemCount = command.Items.Count });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish OrderPlaced event to Event Grid for Order {OrderId}", orderId);
                }
 
                // Trigger Azure Function for order fulfillment orchestration
                var functionUrl = _configuration["Functions:OrderFulfillmentUrl"] ?? "http://localhost:7071/api/orders/fulfill";
                try
                {
                    using var httpClient = _httpClientFactory.CreateClient();
                    var response = await httpClient.PostAsJsonAsync(functionUrl, new { OrderId = orderId });
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully triggered order fulfillment orchestration for order {OrderId}", orderId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to trigger order fulfillment orchestration for order {OrderId}. Status: {Status}", orderId, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error triggering order fulfillment for order {OrderId}", orderId);
                }
 
                var totalDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "[{RequestId}] Checkout completed successfully in {TotalDuration}ms",
                    requestId,
                    totalDuration.TotalMilliseconds);

                return Ok(new ApiResponse<object>
                {
                    IsSuccessful = true,
                    Status = "Success",
                    StatusReason = "Order placed successfully",
                    Data = new { orderId = orderId.ToString() }
                });
            }
            catch (Exception ex)
            {
                var totalDuration = DateTime.UtcNow - startTime;
                _logger.LogError(
                    ex,
                    "[{RequestId}] Exception in order checkout after {TotalDuration}ms: {Message}",
                    requestId,
                    totalDuration.TotalMilliseconds,
                    ex.Message);

                var errorMessage = ex switch
                {
                    ArgumentException => "Invalid order data provided. Please check your order details and try again.",
                    InvalidOperationException => "Unable to process your order. This may be due to insufficient stock or a temporary issue. Please try again.",
                    TimeoutException => "Order processing timed out. Please try again in a moment.",
                    _ => "An unexpected error occurred while processing your order. Please try again or contact support if the issue persists."
                };

                return StatusCode(500, new ApiResponse<object>
                {
                    IsSuccessful = false,
                    Status = "Exception",
                    StatusReason = errorMessage
                });
            }
        }

        [Authorize]
        [HttpGet("my-orders")]
        public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
        {
            try
            {
                var result = await _mediator.Send(new GetMyOrdersQuery());
                return SuccessResponse(result, "Success", "Orders retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve your orders at this time. Please try again or contact support if the issue persists.");
            }
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderDto>> GetById(Guid id)
        {
            try
            {
                var order = await _mediator.Send(new GetOrderByIdQuery { OrderId = id });
                if (order == null)
                {
                    return NotFoundResponse<OrderDto>("Order not found");
                }

                if (!CanAccessOrder(order.UserId))
                {
                    return UnauthorizedResponse<OrderDto>("You do not have access to this order.");
                }

                return SuccessResponse(order, "Success", "Order retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<OrderDto>(ex, "Unable to retrieve the requested order. Please try again or contact support if the issue persists.");
            }
        }

        [Authorize]
        [HttpGet("customer/{customerId:long}")]
        public async Task<ActionResult<List<OrderDto>>> GetByCustomerId(long customerId)
        {
            try
            {
                if (!IsAdmin() && GetCurrentUserId() != customerId)
                {
                    return UnauthorizedResponse<List<OrderDto>>("You do not have access to this customer's orders.");
                }

                var orders = await _mediator.Send(new GetOrdersByCustomerIdQuery { CustomerId = customerId });
                return SuccessResponse(orders, "Success", "Customer orders retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException<List<OrderDto>>(ex, "Unable to retrieve customer orders. Please try again or contact support if the issue persists.");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/update-status")]
        public async Task<ActionResult<object>> UpdateStatus(
            Guid id,
            [FromBody] Ecommerce.Application.Features.Admin.Models.UpdateStatusDto dto,
            [FromServices] ITopicEventSender topicEventSender)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                {
                    return ValidationErrorResponse("Invalid status data.");
                }

                var updated = await _mediator.Send(new UpdateOrderStatusCommand
                {
                    OrderId = id,
                    Status = dto.Status
                });

                if (!updated)
                {
                    return NotFoundResponse("Order not found");
                }

                if (OrderStatuses.TryNormalize(dto.Status, out var normalizedStatus))
                {
                    await topicEventSender.SendAsync(
                        GraphQlEventTopics.OrderStatusChanged,
                        new OrderStatusSubscriptionPayload
                        {
                            OrderId = id,
                            CurrentStatus = normalizedStatus,
                            ChangedAt = DateTime.UtcNow
                        });
                }

                return SuccessResponse(new { success = true }, "Success", "Order status updated successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to update order status. Please check the order state and requested status, then try again.");
            }
        }

        [Authorize]
        [HttpDelete("{id:guid}/cancel")]
        public async Task<ActionResult<object>> Cancel(Guid id, [FromServices] ITopicEventSender topicEventSender)
        {
            try
            {
                var order = await _mediator.Send(new GetOrderByIdQuery { OrderId = id });
                if (order == null)
                {
                    return NotFoundResponse("Order not found");
                }

                if (!CanAccessOrder(order.UserId))
                {
                    return UnauthorizedResponse("You do not have permission to cancel this order.");
                }

                var cancelled = await _mediator.Send(new CancelOrderCommand { OrderId = id });
                if (!cancelled)
                {
                    return NotFoundResponse("Order not found");
                }

                await topicEventSender.SendAsync(
                    GraphQlEventTopics.OrderStatusChanged,
                    new OrderStatusSubscriptionPayload
                    {
                        OrderId = id,
                        CurrentStatus = OrderStatuses.Cancelled,
                        ChangedAt = DateTime.UtcNow
                    });

                return SuccessResponse(new { success = true }, "Success", "Order cancelled successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to cancel this order. Orders that have already shipped cannot be cancelled.");
            }
        }

        private long? GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }

        private bool IsAdmin()
        {
            return User.Claims.Any(claim =>
                claim.Type == ClaimTypes.Role &&
                string.Equals(claim.Value, "Admin", StringComparison.OrdinalIgnoreCase));
        }

        private bool CanAccessOrder(long orderUserId)
        {
            return IsAdmin() || GetCurrentUserId() == orderUserId;
        }
    }
}
