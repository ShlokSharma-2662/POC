using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using Serilog;

namespace Ecommerce.API.Middleware
{
    /// <summary>
    /// Middleware for logging errors to database
    /// Note: This middleware logs errors but does NOT handle them.
    /// Exception handling is done by GlobalExceptionHandlerMiddleware.
    /// </summary>
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Get correlation ID from request/response headers
                var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                    ?? context.Response.Headers["X-Correlation-ID"].FirstOrDefault() 
                    ?? "Unknown";

                // Log to Serilog
                _logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error occurred: {Message}", correlationId, ex.Message);
                Log.Error(ex, "[CorrelationId: {CorrelationId}] Error occurred: {Message}", correlationId, ex.Message);

                // Log to database (fire and forget to avoid blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var error = new ErrorLog
                        {
                            Message = $"[CorrelationId: {correlationId}] {ex.GetType().Name}: {ex.Message}",
                            StackTrace = ex.StackTrace,
                            Path = $"{context.Request.Method} {context.Request.Path}",
                            Timestamp = DateTime.UtcNow,
                            Severity = "Error",
                            UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                            IpAddress = context.Connection.RemoteIpAddress?.ToString()
                        };

                        dbContext.ErrorLogs.Add(error);
                        await dbContext.SaveChangesAsync();
                    }
                    catch (Exception logEx)
                    {
                        // Don't fail if database logging fails
                        _logger.LogWarning(logEx, "Failed to log error to database");
                    }
                });

                // Re-throw to let GlobalExceptionHandlerMiddleware handle the response
                throw;
            }
        }
    }
}
