using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// No-operation implementation of IMessagePublisherService for when RabbitMQ is disabled
    /// </summary>
    public class NoOpMessagePublisherService : IMessagePublisherService
    {
        private readonly ILogger<NoOpMessagePublisherService> _logger;

        public NoOpMessagePublisherService(ILogger<NoOpMessagePublisherService> logger)
        {
            _logger = logger;
        }

        public Task PublishOrderCreatedAsync(Guid orderId, string userId, string userEmail, decimal totalAmount, DateTime createdAt, string orderNumber)
        {
            _logger.LogInformation("Message publishing disabled - OrderCreated event for Order {OrderNumber} would have been published", orderNumber);
            return Task.CompletedTask;
        }

        public Task PublishOrderStatusChangedAsync(Guid orderId, string userId, string userEmail, string oldStatus, string newStatus, DateTime changedAt, string orderNumber)
        {
            _logger.LogInformation("Message publishing disabled - OrderStatusChanged event for Order {OrderNumber} from {OldStatus} to {NewStatus} would have been published", 
                orderNumber, oldStatus, newStatus);
            return Task.CompletedTask;
        }

        public Task PublishPaymentProcessedAsync(Guid orderId, string userId, string userEmail, decimal amount, string paymentMethod, string transactionId, bool isSuccessful, DateTime processedAt, string orderNumber)
        {
            _logger.LogInformation("Message publishing disabled - PaymentProcessed event for Order {OrderNumber}, Success: {IsSuccessful} would have been published", 
                orderNumber, isSuccessful);
            return Task.CompletedTask;
        }

        public Task PublishUserRegisteredAsync(string userId, string email, string firstName, string lastName, DateTime registeredAt, string registrationSource)
        {
            _logger.LogInformation("Message publishing disabled - UserRegistered event for {Email} would have been published", email);
            return Task.CompletedTask;
        }

        public Task PublishPasswordResetRequestedAsync(string userId, string email, string resetToken, DateTime requestedAt, DateTime expiresAt)
        {
            _logger.LogInformation("Message publishing disabled - PasswordResetRequested event for {Email} would have been published", email);
            return Task.CompletedTask;
        }

        public Task PublishEmailSendAsync(string toEmail, string toName, string subject, string htmlContent, string plainTextContent, string emailType, DateTime requestedAt, Guid? correlationId = null)
        {
            _logger.LogInformation("Message publishing disabled - EmailSend event for {EmailType} to {ToEmail} would have been published", emailType, toEmail);
            return Task.CompletedTask;
        }

        public Task PublishAdminNotificationAsync(string notificationType, string title, string message, string severity, DateTime createdAt, string source, object? additionalData = null)
        {
            _logger.LogInformation("Message publishing disabled - AdminNotification event for {NotificationType} would have been published", notificationType);
            return Task.CompletedTask;
        }

        public Task PublishSystemMetricsAlertAsync(string metricName, double currentValue, double thresholdValue, string severity, string endpoint, DateTime detectedAt, string message)
        {
            _logger.LogInformation("Message publishing disabled - SystemMetricsAlert event for {MetricName} would have been published", metricName);
            return Task.CompletedTask;
        }

        public Task PublishUserActivityAsync(string userId, string activity, string details, string ipAddress, string userAgent, DateTime timestamp, string? sessionId = null)
        {
            _logger.LogInformation("Message publishing disabled - UserActivity event for {UserId} - {Activity} would have been published", userId, activity);
            return Task.CompletedTask;
        }
    }
}
