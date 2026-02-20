using Ecommerce.Application.Common.Models;
using Ecommerce.Application.Features.Payments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace Ecommerce.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : BaseController
    {
        private readonly IConfiguration _configuration;

        public PaymentsController(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [Authorize]
        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            try
            {
                Console.WriteLine($"Payment intent request received: Amount={request.Amount}, Currency={request.Currency}");
                
                if (request.Amount <= 0) 
                {
                    Console.WriteLine("Invalid amount provided");
                    return ValidationErrorResponse("Invalid amount");
                }

                Console.WriteLine($"Stripe API Key configured: {!string.IsNullOrEmpty(_configuration["Stripe:SecretKey"])}");

                var options = new PaymentIntentCreateOptions
                {
                    Amount = request.Amount,
                    Currency = request.Currency,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    },
                    Metadata = request.Metadata
                };

                Console.WriteLine("Creating Stripe payment intent...");
                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);
                Console.WriteLine($"Stripe payment intent created: {intent.Id}, ClientSecret: {intent.ClientSecret?.Substring(0, 20)}...");

                var responseData = new { clientSecret = intent.ClientSecret };
                Console.WriteLine($"Creating ApiResponse with data: {System.Text.Json.JsonSerializer.Serialize(responseData)}");
                
                var apiResponse = new ApiResponse<object>
                {
                    IsSuccessful = true,
                    Status = "Success",
                    StatusReason = "Payment intent created successfully",
                    Data = responseData
                };
                
                Console.WriteLine($"Final ApiResponse: {System.Text.Json.JsonSerializer.Serialize(apiResponse)}");
                return Ok(apiResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in payment intent creation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Provide more specific error messages based on exception type
                string errorMessage = ex switch
                {
                    ArgumentException => "Invalid payment information provided. Please check your payment details and try again.",
                    InvalidOperationException => "Payment processing is temporarily unavailable. Please try again in a few moments.",
                    TimeoutException => "Payment request timed out. Please try again.",
                    _ => "An unexpected error occurred while processing your payment. Please try again or contact support if the issue persists."
                };
                
                var errorResponse = new ApiResponse<object>
                {
                    IsSuccessful = false,
                    Status = "Exception",
                    StatusReason = errorMessage,
                    Data = null
                };
                
                return StatusCode(500, errorResponse);
            }
        }
    }
}
