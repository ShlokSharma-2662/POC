using Microsoft.AspNetCore.Http;
using System.Net;

namespace Ecommerce.API.Middleware
{
    public class RequestTimeoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimeoutMiddleware> _logger;
        private readonly TimeSpan _timeout;

        public RequestTimeoutMiddleware(RequestDelegate next, ILogger<RequestTimeoutMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            
            // Get timeout from configuration, default to 5 minutes
            var timeoutString = configuration["TimeoutSettings:RequestTimeout"] ?? "00:05:00";
            _timeout = TimeSpan.Parse(timeoutString);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using var cts = new CancellationTokenSource(_timeout);
            
            try
            {
                // Set the cancellation token in the context
                context.RequestAborted = cts.Token;
                
                await _next(context);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Request timeout for {Path} after {Timeout}", 
                    context.Request.Path, _timeout);
                
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        IsSuccessful = false,
                        Status = "Timeout",
                        StatusReason = "Request timed out. Please try again.",
                        Data = (object?)null
                    };
                    
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
                }
            }
        }
    }
}

