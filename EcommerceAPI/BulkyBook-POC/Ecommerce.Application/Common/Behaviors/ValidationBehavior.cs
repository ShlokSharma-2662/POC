using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Ecommerce.Application.Common.Models;

namespace Ecommerce.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                _logger.LogWarning(
                    "Validation failed for {RequestType}. Errors: {Errors}",
                    typeof(TRequest).Name,
                    failures.Select(f => f.ErrorMessage));

                var errors = failures.Select(f => f.ErrorMessage).ToList();
                var errorMessage = string.Join("; ", errors);

                if (typeof(TResponse) == typeof(ApiResponse))
                {
                    return (TResponse)(object)ApiResponse.ValidationError(errorMessage);
                }

                if (typeof(TResponse) == typeof(ApiResponse<>))
                {
                    return (TResponse)(object)ApiResponse<object>.ValidationError(errorMessage);
                }

                throw new ValidationException(failures);
            }

            return await next();
        }
    }
}
