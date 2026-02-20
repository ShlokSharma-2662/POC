using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Admin.Commands;
using Ecommerce.Application.Features.Admin.Models;
using Ecommerce.Application.Features.Admin.Queries;
using Ecommerce.Application.Features.Metrics.Commands;
using Ecommerce.Application.Features.Metrics.Queries;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("all-orders")]
        public async Task<ActionResult<PagedResult<AdminOrderDto>>> GetAllOrders([FromQuery] GetAllOrdersForAdminQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "All orders retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve orders at this time. Please try again or contact support if the issue persists.");
            }
        }

        [HttpPut("orders/{id}/status")]
        public async Task<ActionResult<object>> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.Status))
                {
                    return ValidationErrorResponse("Invalid status data.");
                }
                var command = new UpdateOrderStatusCommand
                {
                    OrderId = id,
                    Status = dto.Status
                };

                var result = await _mediator.Send(command);
                if (!result) return NotFoundResponse("Order not found");

                return SuccessResponse(new { success = true }, "Success", "Order status updated successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to update order status. Please check the order ID and status value, then try again. If the issue persists, contact support.");
            }
        }

        [HttpGet("metrics")]
        public async Task<ActionResult<PagedResult<SystemMetric>>> GetSystemMetrics([FromQuery] Ecommerce.Application.Features.Metrics.Queries.GetSystemMetricsQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "System metrics retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve system metrics at this time. Please try again or contact support if the issue persists.");
            }
        }

        [HttpPost("log")]
        public async Task<IActionResult> LogMetric([FromBody] LogMetricCommand command)
        {
            try
            {
                if (command == null)
                {
                    return ValidationErrorResponse("Invalid metric data.");
                }
                await _mediator.Send(command);
                return SuccessResponse("Success", "Metric logged successfully.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to log metric. Please check your metric data and try again. If the issue persists, contact support.");
            }
            
        }

        [HttpGet("errors")]
        public async Task<ActionResult<PagedResult<ErrorLog>>> GetErrorLogs([FromQuery] GetAllErrorLogsQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "Error logs retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve error logs at this time. Please try again or contact support if the issue persists.");
            }
        }



        [HttpPost("metrics/seed")]
        public async Task<ActionResult<object>> SeedMetrics()
        {
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var sampleMetrics = new List<SystemMetric>
                {
                    new SystemMetric
                    {
                        Endpoint = "/api/products",
                        Method = "GET",
                        ResponseTimeMs = 150,
                        StatusCode = 200,
                        Timestamp = DateTime.Now.AddMinutes(-10),
                        IsThresholdExceeded = false
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/products",
                        Method = "GET",
                        ResponseTimeMs = 180,
                        StatusCode = 500,
                        Timestamp = DateTime.Now.AddMinutes(-9),
                        IsThresholdExceeded = false
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/orders",
                        Method = "POST",
                        ResponseTimeMs = 250,
                        StatusCode = 201,
                        Timestamp = DateTime.Now.AddMinutes(-8),
                        IsThresholdExceeded = false
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/orders",
                        Method = "POST",
                        ResponseTimeMs = 300,
                        StatusCode = 400,
                        Timestamp = DateTime.Now.AddMinutes(-7),
                        IsThresholdExceeded = false
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/auth/login",
                        Method = "POST",
                        ResponseTimeMs = 1200,
                        StatusCode = 200,
                        Timestamp = DateTime.Now.AddMinutes(-6),
                        IsThresholdExceeded = true
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/auth/login",
                        Method = "POST",
                        ResponseTimeMs = 150,
                        StatusCode = 401,
                        Timestamp = DateTime.Now.AddMinutes(-5),
                        IsThresholdExceeded = false
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/products/1",
                        Method = "GET",
                        ResponseTimeMs = 80,
                        StatusCode = 200,
                        Timestamp = DateTime.Now.AddMinutes(-4),
                        IsThresholdExceeded = false
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/products/1",
                        Method = "GET",
                        ResponseTimeMs = 200,
                        StatusCode = 404,
                        Timestamp = DateTime.Now.AddMinutes(-3),
                        IsThresholdExceeded = false
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/cart",
                        Method = "GET",
                        ResponseTimeMs = 300,
                        StatusCode = 401,
                        Timestamp = DateTime.Now.AddMinutes(-2),
                        IsThresholdExceeded = false
                    },
                    new SystemMetric
                    {
                        Endpoint = "/api/cart",
                        Method = "POST",
                        ResponseTimeMs = 450,
                        StatusCode = 200,
                        Timestamp = DateTime.Now.AddMinutes(-1),
                        IsThresholdExceeded = false
                    }
                };
                
                await dbContext.SystemMetrics.AddRangeAsync(sampleMetrics);
                await dbContext.SaveChangesAsync();
                
                return SuccessResponse(new { count = sampleMetrics.Count }, "Success", "Sample metrics seeded successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to seed metrics. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers([FromQuery] GetAllUsersQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "Users retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve users at this time. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<AdminUserDto>> GetUserById(long id)
        {
            try
            {
                var result = await _mediator.Send(new GetUserByIdQuery { UserId = id });
                if (result == null) return NotFoundResponse("User not found");
                return SuccessResponse(result, "Success", "User retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve user information. Please check the user ID and try again. If the issue persists, contact support.");
            }
        }


        [HttpPost("users/assign-role")]
        public async Task<ActionResult<object>> AssignRole([FromBody] AssignRoleDto dto)
        {
            try
            {
                if (dto == null || dto.UserId <= 0 || string.IsNullOrWhiteSpace(dto.Role))
                    return ValidationErrorResponse("Invalid request.");

                var result = await _mediator.Send(new AssignUserRoleCommand { UserId = dto.UserId, Role = dto.Role });
                if (!result) return NotFoundResponse("User not found");
                return SuccessResponse(new { success = true }, "Success", "Role assigned successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to assign role. Please check the user ID and role, then try again. If the issue persists, contact support.");
            }
        }

        [HttpPost("users/reset-password")]
        public async Task<ActionResult<object>> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (dto == null || dto.UserId <= 0 || string.IsNullOrWhiteSpace(dto.NewPassword))
                    return ValidationErrorResponse("Invalid request.");

                var result = await _mediator.Send(new ResetUserPasswordCommand { UserId = dto.UserId, NewPassword = dto.NewPassword });
                if (!result) return NotFoundResponse("User not found");
                return SuccessResponse(new { success = true }, "Success", "Password reset successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to reset password. Please check the user ID and new password, then try again. If the issue persists, contact support.");
            }
        }

        [HttpPost("users/deactivate")]
        public async Task<ActionResult<object>> Deactivate([FromBody] DeactivateUserDto dto)
        {
            try
            {
                if (dto == null || dto.UserId <= 0)
                    return ValidationErrorResponse("Invalid request.");

                var result = await _mediator.Send(new DeactivateUserCommand { UserId = dto.UserId });
                if (!result) return NotFoundResponse("User not found");
                return SuccessResponse(new { success = true }, "Success", "User deactivated successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to deactivate user. Please check the user ID and try again. If the issue persists, contact support.");
            }
        }

        [HttpPost("users/activate")]
        public async Task<ActionResult<object>> Activate([FromBody] ActivateUserDto dto)
        {
            try
            {
                if (dto == null || dto.UserId <= 0)
                    return ValidationErrorResponse("Invalid request.");

                var result = await _mediator.Send(new ActivateUserCommand { UserId = dto.UserId });
                if (!result) return NotFoundResponse("User not found");
                return SuccessResponse(new { success = true }, "Success", "User activated successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to activate user. Please check the user ID and try again. If the issue persists, contact support.");
            }
        }

        [HttpGet("revenue/report")]
        public async Task<ActionResult<RevenueReportDto>> GetRevenueReport([FromQuery] GetRevenueReportQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "Revenue report retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve revenue report at this time. Please try again or contact support if the issue persists.");
            }
        }

        [HttpGet("revenue/summary")]
        public async Task<ActionResult<RevenueSummaryDto>> GetRevenueSummary([FromQuery] GetRevenueSummaryQuery query)
        {
            try
            {
                var result = await _mediator.Send(query);
                return SuccessResponse(result, "Success", "Revenue summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Unable to retrieve revenue summary at this time. Please try again or contact support if the issue persists.");
            }
        }
    }
}