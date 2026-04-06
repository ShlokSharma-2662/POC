using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecommerce.Application.Common.Behaviors
{
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

        public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var timer = new Stopwatch();
            timer.Start();

            var response = await next();

            timer.Stop();

            var elapsedMilliseconds = timer.ElapsedMilliseconds;

            if (elapsedMilliseconds > 500)
            {
                _logger.LogWarning(
                    "Long running request: {RequestType} ({ElapsedMilliseconds}ms) {@Request}",
                    typeof(TRequest).Name,
                    elapsedMilliseconds,
                    request);
            }
            else
            {
                _logger.LogInformation(
                    "Request: {RequestType} completed in {ElapsedMilliseconds}ms",
                    typeof(TRequest).Name,
                    elapsedMilliseconds);
            }

            return response;
        }
    }
}
