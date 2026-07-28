using Ecommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Ecommerce.API.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimitingService _rateLimitingService;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        public RateLimitingMiddleware(RequestDelegate next, IRateLimitingService rateLimitingService, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _rateLimitingService = rateLimitingService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var endpoint = $"{context.Request.Method}:{context.Request.Path}";
                var clientId = GetClientIdentifier(context);
                
                var rateLimitResult = await _rateLimitingService.CheckRateLimitAsync(endpoint, clientId);
                
                context.Response.Headers["X-RateLimit-Limit"] = rateLimitResult.Limit.ToString(CultureInfo.InvariantCulture);
                context.Response.Headers["X-RateLimit-Remaining"] = rateLimitResult.Remaining.ToString(CultureInfo.InvariantCulture);
                var resetTimeUtc = rateLimitResult.ResetTime.Kind == DateTimeKind.Utc
                    ? rateLimitResult.ResetTime
                    : rateLimitResult.ResetTime.ToUniversalTime();
                var resetTimestamp = new DateTimeOffset(resetTimeUtc).ToUnixTimeSeconds();
                context.Response.Headers["X-RateLimit-Reset"] = resetTimestamp.ToString(CultureInfo.InvariantCulture);
                context.Response.Headers["X-RateLimit-Reset-At"] = resetTimeUtc.ToString("O", CultureInfo.InvariantCulture);

                if (!rateLimitResult.IsAllowed)
                {
                    _logger.LogWarning("Rate limit exceeded for {ClientId} on {Endpoint}", clientId, endpoint);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    var retryAfterSeconds = Math.Max(0, (int)Math.Ceiling((resetTimeUtc - DateTime.UtcNow).TotalSeconds));
                    context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                    
                    var response = new
                    {
                        error = "Rate limit exceeded",
                        message = "Too many requests. Please try again later.",
                        retryAfter = retryAfterSeconds
                    };
                    
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate limiting middleware. Allowing request to proceed to prevent service disruption.");
                await _next(context);
            }
        }

        private string GetClientIdentifier(HttpContext context)
        {
            var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }
            
            return clientId;
        }
    }
}
