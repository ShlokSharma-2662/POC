using Ecommerce.Domain.Common.Messaging.Contracts;
using Ecommerce.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Consumer for handling order created events
    /// </summary>
    public class OrderCreatedConsumer : IConsumer<IOrderCreatedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderCreatedConsumer> _logger;

        public OrderCreatedConsumer(IEmailService emailService, ILogger<OrderCreatedConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IOrderCreatedEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("Processing order created event for Order {OrderNumber}", message.OrderNumber);

                // Send order confirmation email
                var subject = $"Order Confirmation - {message.OrderNumber}";
                var htmlContent = $@"
                    <h2>Order Confirmation</h2>
                    <p>Thank you for your order!</p>
                    <p><strong>Order Number:</strong> {message.OrderNumber}</p>
                    <p><strong>Order Date:</strong> {message.CreatedAt:yyyy-MM-dd HH:mm:ss}</p>
                    <p><strong>Total Amount:</strong> ${message.TotalAmount:F2}</p>
                    <p>We'll send you another email when your order ships.</p>
                    <p>Thank you for shopping with us!</p>";

                var plainTextContent = $"Order Confirmation. Thank you for your order! Order Number: {message.OrderNumber}, Order Date: {message.CreatedAt:yyyy-MM-dd HH:mm:ss}, Total Amount: ${message.TotalAmount:F2}. We'll send you another email when your order ships. Thank you for shopping with us!";

                await _emailService.SendEmailAsync(
                    message.UserEmail,
                    "Customer",
                    subject,
                    htmlContent,
                    plainTextContent);

                _logger.LogInformation("Successfully sent order confirmation email for Order {OrderNumber}", message.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email for Order {OrderNumber}. Error: {Error}", 
                    message.OrderNumber, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Consumer for handling order status change events
    /// </summary>
    public class OrderStatusChangedConsumer : IConsumer<IOrderStatusChangedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderStatusChangedConsumer> _logger;

        public OrderStatusChangedConsumer(IEmailService emailService, ILogger<OrderStatusChangedConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IOrderStatusChangedEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("Processing order status change event for Order {OrderNumber} from {OldStatus} to {NewStatus}", 
                    message.OrderNumber, message.OldStatus, message.NewStatus);

                // Only send email for certain status changes
                if (ShouldSendEmail(message.NewStatus))
                {
                    var subject = $"Order Update - {message.OrderNumber}";
                    var statusMessage = GetStatusMessage(message.NewStatus);
                    
                    var htmlContent = $@"
                        <h2>Order Status Update</h2>
                        <p>Your order status has been updated.</p>
                        <p><strong>Order Number:</strong> {message.OrderNumber}</p>
                        <p><strong>New Status:</strong> {message.NewStatus}</p>
                        <p><strong>Updated:</strong> {message.ChangedAt:yyyy-MM-dd HH:mm:ss}</p>
                        <p>{statusMessage}</p>
                        <p>Thank you for your business!</p>";

                    var plainTextContent = $"Order Status Update. Your order status has been updated. Order Number: {message.OrderNumber}, New Status: {message.NewStatus}, Updated: {message.ChangedAt:yyyy-MM-dd HH:mm:ss}. {statusMessage}. Thank you for your business!";

                    await _emailService.SendEmailAsync(
                        message.UserEmail,
                        "Customer",
                        subject,
                        htmlContent,
                        plainTextContent);

                    _logger.LogInformation("Successfully sent order status update email for Order {OrderNumber}", message.OrderNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order status update email for Order {OrderNumber}. Error: {Error}", 
                    message.OrderNumber, ex.Message);
                throw;
            }
        }

        private static bool ShouldSendEmail(string status)
        {
            return status.ToLower() switch
            {
                "shipped" => true,
                "delivered" => true,
                "cancelled" => true,
                "refunded" => true,
                _ => false
            };
        }

        private static string GetStatusMessage(string status)
        {
            return status.ToLower() switch
            {
                "shipped" => "Your order has been shipped and is on its way to you!",
                "delivered" => "Your order has been delivered. We hope you enjoy your purchase!",
                "cancelled" => "Your order has been cancelled. If you have any questions, please contact our support team.",
                "refunded" => "Your order has been refunded. The refund will be processed within 3-5 business days.",
                _ => "Your order status has been updated."
            };
        }
    }

    /// <summary>
    /// Consumer for handling payment processed events
    /// </summary>
    public class PaymentProcessedConsumer : IConsumer<IPaymentProcessedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentProcessedConsumer> _logger;

        public PaymentProcessedConsumer(IEmailService emailService, ILogger<PaymentProcessedConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IPaymentProcessedEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("Processing payment processed event for Order {OrderNumber}, Success: {IsSuccessful}", 
                    message.OrderNumber, message.IsSuccessful);

                if (message.IsSuccessful)
                {
                    var subject = $"Payment Confirmation - {message.OrderNumber}";
                    var htmlContent = $@"
                        <h2>Payment Confirmation</h2>
                        <p>Your payment has been successfully processed!</p>
                        <p><strong>Order Number:</strong> {message.OrderNumber}</p>
                        <p><strong>Amount:</strong> ${message.Amount:F2}</p>
                        <p><strong>Payment Method:</strong> {message.PaymentMethod}</p>
                        <p><strong>Transaction ID:</strong> {message.TransactionId}</p>
                        <p><strong>Processed:</strong> {message.ProcessedAt:yyyy-MM-dd HH:mm:ss}</p>
                        <p>Thank you for your payment!</p>";

                    var plainTextContent = $"Payment Confirmation. Your payment has been successfully processed! Order Number: {message.OrderNumber}, Amount: ${message.Amount:F2}, Payment Method: {message.PaymentMethod}, Transaction ID: {message.TransactionId}, Processed: {message.ProcessedAt:yyyy-MM-dd HH:mm:ss}. Thank you for your payment!";

                    await _emailService.SendEmailAsync(
                        message.UserEmail,
                        "Customer",
                        subject,
                        htmlContent,
                        plainTextContent);

                    _logger.LogInformation("Successfully sent payment confirmation email for Order {OrderNumber}", message.OrderNumber);
                }
                else
                {
                    // Send payment failure notification
                    var subject = $"Payment Failed - {message.OrderNumber}";
                    var htmlContent = $@"
                        <h2>Payment Failed</h2>
                        <p>We were unable to process your payment.</p>
                        <p><strong>Order Number:</strong> {message.OrderNumber}</p>
                        <p><strong>Amount:</strong> ${message.Amount:F2}</p>
                        <p><strong>Payment Method:</strong> {message.PaymentMethod}</p>
                        <p>Please try again or contact our support team for assistance.</p>";

                    var plainTextContent = $"Payment Failed. We were unable to process your payment. Order Number: {message.OrderNumber}, Amount: ${message.Amount:F2}, Payment Method: {message.PaymentMethod}. Please try again or contact our support team for assistance.";

                    await _emailService.SendEmailAsync(
                        message.UserEmail,
                        "Customer",
                        subject,
                        htmlContent,
                        plainTextContent);

                    _logger.LogInformation("Successfully sent payment failure notification for Order {OrderNumber}", message.OrderNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment notification email for Order {OrderNumber}. Error: {Error}", 
                    message.OrderNumber, ex.Message);
                throw;
            }
        }
    }
}
