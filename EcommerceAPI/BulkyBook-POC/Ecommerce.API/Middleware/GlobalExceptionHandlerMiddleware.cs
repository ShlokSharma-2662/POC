using Ecommerce.Application.Common.Models;
using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System.Net;
using System.Text.Json;

namespace Ecommerce.API.Middleware
{
    /// <summary>
    /// Global exception handler middleware that converts all exceptions to ApiResponse format
    /// Includes correlation ID and maps exceptions to appropriate HTTP status codes
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Get correlation ID from request headers or generate new one
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                ?? Guid.NewGuid().ToString();
            
            // Add correlation ID to response headers
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            // Log the exception with correlation ID
            _logger.LogError(
                exception,
                "[CorrelationId: {CorrelationId}] Exception occurred: {ExceptionType} - {Message}",
                correlationId,
                exception.GetType().Name,
                exception.Message);

            // Determine HTTP status code and error message
            var (statusCode, status, message) = MapExceptionToResponse(exception);

            // Set response status code
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            // Create ApiResponse
            var response = new ApiResponse<object>
            {
                IsSuccessful = false,
                Status = status,
                StatusReason = message,
                Data = new
                {
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow,
                    // Only include stack trace in development
                    StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
                    // Only include inner exception details in development
                    InnerException = _environment.IsDevelopment() && exception.InnerException != null
                        ? new
                        {
                            Type = exception.InnerException.GetType().Name,
                            Message = exception.InnerException.Message
                        }
                        : null
                }
            };

            // Serialize and write response
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var json = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(json);
        }

        private (HttpStatusCode statusCode, string status, string message) MapExceptionToResponse(Exception exception)
        {
            return exception switch
            {
                // Validation errors - 400 Bad Request
                ArgumentException argEx => (
                    HttpStatusCode.BadRequest,
                    "ValidationError",
                    argEx.Message ?? "Invalid input provided. Please check your request and try again."
                ),

                // Business logic errors - 409 Conflict
                InvalidOperationException invOpEx => (
                    HttpStatusCode.Conflict,
                    "BusinessRuleViolation",
                    invOpEx.Message ?? "A business rule violation occurred. Please check your request and try again."
                ),

                // Not found errors - 404 Not Found
                KeyNotFoundException keyNotFoundEx => (
                    HttpStatusCode.NotFound,
                    "NotFound",
                    keyNotFoundEx.Message ?? "The requested resource was not found."
                ),

                // Unauthorized errors - 401 Unauthorized
                UnauthorizedAccessException unauthEx => (
                    HttpStatusCode.Unauthorized,
                    "Unauthorized",
                    "Access denied. Please authenticate and try again."
                ),

                // OAuth-specific exceptions - 400 Bad Request
                OAuthException oauthEx when oauthEx is InvalidGrantException => (
                    HttpStatusCode.BadRequest,
                    "InvalidGrant",
                    oauthEx.Message ?? "OAuth authorization failed. The authorization code may be invalid or expired."
                ),

                OAuthException oauthEx when oauthEx is TokenExchangeException => (
                    HttpStatusCode.BadGateway,
                    "TokenExchangeFailed",
                    oauthEx.Message ?? "OAuth token exchange failed. Please try again."
                ),

                OAuthException oauthEx when oauthEx is UserInfoException => (
                    HttpStatusCode.BadGateway,
                    "UserInfoFailed",
                    oauthEx.Message ?? "Failed to retrieve user information. Please try again."
                ),

                OAuthException oauthEx => (
                    HttpStatusCode.BadGateway,
                    "OAuthError",
                    oauthEx.Message ?? "OAuth service error. Please try again later."
                ),

                // Timeout errors - 504 Gateway Timeout
                TimeoutException timeoutEx => (
                    HttpStatusCode.GatewayTimeout,
                    "Timeout",
                    "The request timed out. Please try again."
                ),

                TaskCanceledException taskCancelEx => (
                    HttpStatusCode.GatewayTimeout,
                    "Timeout",
                    "The request was cancelled or timed out. Please try again."
                ),

                // Circuit breaker errors - 503 Service Unavailable
                BrokenCircuitException circuitEx => (
                    HttpStatusCode.ServiceUnavailable,
                    "ServiceUnavailable",
                    "The service is temporarily unavailable. Please try again in a few moments."
                ),

                // Database/Entity Framework errors - 500 Internal Server Error
                Microsoft.EntityFrameworkCore.DbUpdateException dbEx => (
                    HttpStatusCode.InternalServerError,
                    "DatabaseError",
                    "A database error occurred. Please try again or contact support if the issue persists."
                ),

                // HttpRequestException - 502 Bad Gateway or 503 Service Unavailable
                HttpRequestException httpEx => (
                    httpEx.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                        ? HttpStatusCode.GatewayTimeout
                        : HttpStatusCode.BadGateway,
                    "ExternalServiceError",
                    "An external service error occurred. Please try again later."
                ),

                // Default - 500 Internal Server Error
                _ => (
                    HttpStatusCode.InternalServerError,
                    "Exception",
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred. Please try again or contact support if the issue persists."
                )
            };
        }
    }
}

