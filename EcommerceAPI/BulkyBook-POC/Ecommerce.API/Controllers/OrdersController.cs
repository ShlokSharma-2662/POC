using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Orders.Commands;
using Ecommerce.Application.Features.Orders.Models;
using Ecommerce.Application.Features.Orders.Queries;
using Ecommerce.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Ecommerce.API.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly IMessagePublisherService _messagePublisher;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IMediator mediator, IMessagePublisherService messagePublisher, ILogger<OrdersController> logger)
        {
            _mediator = mediator;
            _messagePublisher = messagePublisher;
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
                _logger.LogInformation("[{RequestId}] Order checkout request received: FullName={FullName}, Items={ItemCount}", 
                    requestId, command?.FullName, command?.Items?.Count ?? 0);
                
                if (command == null)
                {
                    Console.WriteLine("Order command is null");
                    var errorResponse = new ApiResponse<object>
                    {
                        IsSuccessful = false,
                        Status = "ValidationError",
                        StatusReason = "Invalid order data.",
                        Data = null
                    };
                    return BadRequest(errorResponse);
                }
                
                if (command.Items == null || !command.Items.Any())
                {
                    Console.WriteLine("Order has no items");
                    var errorResponse = new ApiResponse<object>
                    {
                        IsSuccessful = false,
                        Status = "ValidationError",
                        StatusReason = "Order must contain at least one item.",
                        Data = null
                    };
                    return BadRequest(errorResponse);
                }
                
                if (string.IsNullOrWhiteSpace(command.FullName) || string.IsNullOrWhiteSpace(command.Address) || string.IsNullOrWhiteSpace(command.PhoneNumber))
                {
                    Console.WriteLine("Missing required fields");
                    var errorResponse = new ApiResponse<object>
                    {
                        IsSuccessful = false,
                        Status = "ValidationError",
                        StatusReason = "Full name, address, and phone number are required.",
                        Data = null
                    };
                    return BadRequest(errorResponse);
                }
                
                _logger.LogInformation("[{RequestId}] Sending order command to mediator...", requestId);
                var mediatorStartTime = DateTime.UtcNow;
                var orderId = await _mediator.Send(command);
                var mediatorDuration = DateTime.UtcNow - mediatorStartTime;
                _logger.LogInformation("[{RequestId}] Order created successfully with ID: {OrderId} in {Duration}ms", 
                    requestId, orderId, mediatorDuration.TotalMilliseconds);
                
                // Publish order created event to RabbitMQ
                try
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown@example.com";
                    var orderNumber = $"ORD-{orderId.ToString().Substring(0, 8).ToUpper()}";
                    // Calculate total amount - we'll need to get prices from database
                    // For now, set to 0 as this is just for the message
                    var totalAmount = 0m;
                    
                    await _messagePublisher.PublishOrderCreatedAsync(
                        orderId, 
                        userId, 
                        userEmail, 
                        totalAmount, 
                        DateTime.UtcNow, 
                        orderNumber);
                    
                    Console.WriteLine($"Published OrderCreated event for Order {orderNumber}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to publish OrderCreated event: {ex.Message}");
                    // Don't fail the order creation if message publishing fails
                }
                
                var responseData = new { orderId = orderId.ToString() }; // Convert Guid to string
                Console.WriteLine($"Creating ApiResponse with data: {System.Text.Json.JsonSerializer.Serialize(responseData)}");
                
                var totalDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation("[{RequestId}] Checkout completed successfully in {TotalDuration}ms", 
                    requestId, totalDuration.TotalMilliseconds);
                
                var apiResponse = new ApiResponse<object>
                {
                    IsSuccessful = true,
                    Status = "Success",
                    StatusReason = "Order placed successfully",
                    Data = responseData
                };
                
                return Ok(apiResponse);
            }
            catch (Exception ex)
            {
                var totalDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "[{RequestId}] Exception in order checkout after {TotalDuration}ms: {Message}", 
                    requestId, totalDuration.TotalMilliseconds, ex.Message);
                
                // Provide more specific error messages based on exception type
                string errorMessage = ex switch
                {
                    ArgumentException => "Invalid order data provided. Please check your order details and try again.",
                    InvalidOperationException => "Unable to process your order. This may be due to insufficient stock or a temporary issue. Please try again.",
                    TimeoutException => "Order processing timed out. Please try again in a moment.",
                    _ => "An unexpected error occurred while processing your order. Please try again or contact support if the issue persists."
                };
                
                var errorResponse = new ApiResponse<object>
                {
                    IsSuccessful = false,
                    Status = "Exception",
                    StatusReason = errorMessage,
                    Data = null
                };
                
                return StatusCode(500, errorResponse);
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
    }
}
