using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Payments.Models;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : BaseController
    {
        private readonly ISecureCheckoutPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            ISecureCheckoutPaymentService paymentService,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent(
            [FromBody] CreatePaymentIntentRequest? request,
            CancellationToken cancellationToken)
        {
            try
            {
                var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdValue, out var userId))
                {
                    return UnauthorizedResponse("A valid authenticated user is required.");
                }

                if (request?.Items == null || request.Items.Count == 0)
                {
                    return ValidationErrorResponse("The cart must contain at least one item.");
                }

                var result = await _paymentService.CreatePaymentIntentAsync(
                    userId,
                    request.Items
                        .Select(item => new PaymentCartItem(item.ProductId, item.Quantity))
                        .ToArray(),
                    request.CheckoutReference,
                    cancellationToken);

                return Ok(new ApiResponse<object>
                {
                    IsSuccessful = true,
                    Status = "Success",
                    StatusReason = "Payment intent created from the current server price.",
                    Data = new
                    {
                        paymentIntentId = result.PaymentIntentId,
                        clientSecret = result.ClientSecret,
                        amount = result.AmountMinor,
                        currency = result.Currency,
                        cartFingerprint = result.CartFingerprint
                    }
                });
            }
            catch (PaymentValidationException ex)
            {
                return ValidationErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create a secure payment intent for the current user.");
                return StatusCode(502, new ApiResponse<object>
                {
                    IsSuccessful = false,
                    Status = "PaymentProviderError",
                    StatusReason = "Secure payment is temporarily unavailable. Please try again."
                });
            }
        }
    }
}
