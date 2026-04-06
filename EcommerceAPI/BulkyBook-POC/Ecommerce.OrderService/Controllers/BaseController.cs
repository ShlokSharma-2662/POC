using Ecommerce.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Ecommerce.OrderService.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected ActionResult<T> SuccessResponse<T>(T data, string status = "Success", string statusReason = "Operation completed successfully")
        {
            return Ok(ApiResponse<T>.Success(data, status, statusReason));
        }

        protected ActionResult SuccessResponse(string status = "Success", string statusReason = "Operation completed successfully", object? data = null)
        {
            return Ok(ApiResponse.Success(status, statusReason, data));
        }

        protected ActionResult<T> ErrorResponse<T>(string status = "Error", string statusReason = "An error occurred during the operation")
        {
            return BadRequest(ApiResponse<T>.Failure(status, statusReason));
        }

        protected ActionResult ErrorResponse(string status = "Error", string statusReason = "An error occurred during the operation")
        {
            return BadRequest(ApiResponse.Failure(status, statusReason));
        }

        protected ActionResult<T> ExceptionResponse<T>(Exception ex, string? statusReason = null)
        {
            Log.Error(ex, "❌ Exception occurred in {ControllerName}", GetType().Name);
            var message = statusReason ?? ex.Message;
            return StatusCode(500, ApiResponse<T>.Exception(message));
        }

        protected ActionResult ExceptionResponse(Exception ex, string? statusReason = null)
        {
            Log.Error(ex, "❌ Exception occurred in {ControllerName}", GetType().Name);
            var message = statusReason ?? ex.Message;
            return StatusCode(500, ApiResponse.Exception(message));
        }

        protected ActionResult<T> ValidationErrorResponse<T>(string statusReason = "Validation failed")
        {
            return BadRequest(ApiResponse<T>.ValidationError(statusReason));
        }

        protected ActionResult ValidationErrorResponse(string statusReason = "Validation failed")
        {
            return BadRequest(ApiResponse.ValidationError(statusReason));
        }

        protected ActionResult<T> NotFoundResponse<T>(string statusReason = "The requested resource was not found")
        {
            return NotFound(ApiResponse<T>.NotFound(statusReason));
        }

        protected ActionResult NotFoundResponse(string statusReason = "The requested resource was not found")
        {
            return NotFound(ApiResponse.NotFound(statusReason));
        }

        protected ActionResult<T> UnauthorizedResponse<T>(string statusReason = "Access denied")
        {
            return Unauthorized(ApiResponse<T>.Unauthorized(statusReason));
        }

        protected ActionResult UnauthorizedResponse(string statusReason = "Access denied")
        {
            return Unauthorized(ApiResponse.Unauthorized(statusReason));
        }

        protected ActionResult<T> HandleException<T>(Exception ex, string? customMessage = null)
        {
            if (ex is ArgumentException)
            {
                return ValidationErrorResponse<T>(customMessage ?? ex.Message);
            }
            if (ex is InvalidOperationException)
            {
                // Use HTTP 409 Conflict for business rule violations like stock exceeded
                return StatusCode(409, ApiResponse<T>.ValidationError(customMessage ?? ex.Message));
            }
            
            return ExceptionResponse<T>(ex, customMessage);
        }

        protected ActionResult HandleException(Exception ex, string? customMessage = null)
        {
            if (ex is ArgumentException)
            {
                return ValidationErrorResponse(customMessage ?? ex.Message);
            }
            if (ex is InvalidOperationException)
            {
                return StatusCode(409, ApiResponse.ValidationError(customMessage ?? ex.Message));
            }
            
            return ExceptionResponse(ex, customMessage);
        }
    }
}
