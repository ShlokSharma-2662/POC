using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Net;
using System.Net.Http;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// Service for creating and managing resilience policies (retry, circuit breaker, timeout)
    /// </summary>
    public interface IResiliencePolicyService
    {
        IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy();
        IAsyncPolicy<HttpResponseMessage> GetHttpCircuitBreakerPolicy();
        IAsyncPolicy<T> GetRetryPolicy<T>();
        IAsyncPolicy<T> GetCircuitBreakerPolicy<T>();
    }

    public class ResiliencePolicyService : IResiliencePolicyService
    {
        private readonly ILogger<ResiliencePolicyService> _logger;

        public ResiliencePolicyService(ILogger<ResiliencePolicyService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates an HTTP retry policy with exponential backoff
        /// Retries on transient HTTP errors (5xx, 408, 429)
        /// </summary>
        public IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
        {
            return Policy
                .HandleResult<HttpResponseMessage>(r =>
                    r.StatusCode == HttpStatusCode.InternalServerError ||
                    r.StatusCode == HttpStatusCode.BadGateway ||
                    r.StatusCode == HttpStatusCode.ServiceUnavailable ||
                    r.StatusCode == HttpStatusCode.GatewayTimeout ||
                    r.StatusCode == HttpStatusCode.RequestTimeout ||
                    r.StatusCode == (HttpStatusCode)429) // Too Many Requests
                .Or<HttpRequestException>()
                .Or<TaskCanceledException>() // Timeout
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2s, 4s, 8s
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var status = outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.GetType().Name ?? "Unknown";
                        _logger.LogWarning(
                            "Retry {RetryCount} after {Delay}s. Status: {Status}",
                            retryCount,
                            timespan.TotalSeconds,
                            status);
                    });
        }

        /// <summary>
        /// Creates an HTTP circuit breaker policy
        /// Opens circuit after 5 consecutive failures, stays open for 30 seconds
        /// </summary>
        public IAsyncPolicy<HttpResponseMessage> GetHttpCircuitBreakerPolicy()
        {
            return Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (result, duration) =>
                    {
                        var status = result.Result?.StatusCode.ToString() ?? result.Exception?.GetType().Name ?? "Unknown";
                        _logger.LogWarning(
                            "Circuit breaker opened for {Duration}s. Status: {Status}",
                            duration.TotalSeconds,
                            status);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset - service is healthy again");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker half-open - testing if service recovered");
                    });
        }

        /// <summary>
        /// Creates a generic retry policy for non-HTTP operations
        /// </summary>
        public IAsyncPolicy<T> GetRetryPolicy<T>()
        {
            return Policy<T>
                .Handle<Exception>(ex => !(ex is InvalidOperationException)) // Don't retry business logic errors
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} after {Delay}s. Error: {Error}",
                            retryCount,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? "Unknown error");
                    });
        }

        /// <summary>
        /// Creates a generic circuit breaker policy for non-HTTP operations
        /// </summary>
        public IAsyncPolicy<T> GetCircuitBreakerPolicy<T>()
        {
            return Policy<T>
                .Handle<Exception>(ex => !(ex is InvalidOperationException))
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (result, duration) =>
                    {
                        var errorMessage = result.Exception?.Message ?? "Unknown error";
                        _logger.LogWarning(
                            "Circuit breaker opened for {Duration}s. Error: {Error}",
                            duration.TotalSeconds,
                            errorMessage);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("Circuit breaker half-open");
                    });
        }
    }
}

