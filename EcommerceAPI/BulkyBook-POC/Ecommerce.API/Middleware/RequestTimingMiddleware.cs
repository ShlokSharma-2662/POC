using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Persistence;
using System.Diagnostics;

namespace Ecommerce.API.Middleware
{
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        private const long ApiThresholdMs = 1000;

        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                _logger.LogInformation($"🔄 Request: {context.Request.Method} {context.Request.Path}");

                var stopwatch = Stopwatch.StartNew();
                await _next(context);
                stopwatch.Stop();

                var elapsedMs = stopwatch.ElapsedMilliseconds;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var metric = new SystemMetric
                {
                    Endpoint = context.Request.Path,
                    Method = context.Request.Method,
                    ResponseTimeMs = elapsedMs,
                    StatusCode = context.Response.StatusCode,
                    Timestamp = DateTime.Now,
                    IsThresholdExceeded = elapsedMs > ApiThresholdMs,
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    IpAddress = context.Connection.RemoteIpAddress?.ToString()
                };

                await dbContext.SystemMetrics.AddAsync(metric);
                _logger.LogInformation($"📊 Logged metric: {context.Request.Method} {context.Request.Path} - {elapsedMs}ms");

                if (elapsedMs > ApiThresholdMs)
                {
                    _logger.LogWarning($"⚠️ High response time: {elapsedMs} ms on {context.Request.Path}");

                    await dbContext.ApiAlerts.AddAsync(new ApiAlert
                    {
                        Path = context.Request.Path,
                        ResponseTimeMs = elapsedMs,
                        Timestamp = DateTime.Now
                    });
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"💾 Metrics saved to database successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error logging request metrics. This may be due to database connection issues or insufficient permissions. Request processing will continue normally.");
            }
        }

    }
}
