using Ecommerce.Domain.Common.Messaging.Contracts;

namespace Ecommerce.Domain.Interfaces
{
    /// <summary>
    /// Service for publishing messages to RabbitMQ
    /// </summary>
    public interface IMessagePublisherService
    {
        Task PublishOrderCreatedAsync(Guid orderId, string userId, string userEmail, decimal totalAmount, DateTime createdAt, string orderNumber);
        Task PublishOrderStatusChangedAsync(Guid orderId, string userId, string userEmail, string oldStatus, string newStatus, DateTime changedAt, string orderNumber);
        Task PublishPaymentProcessedAsync(Guid orderId, string userId, string userEmail, decimal amount, string paymentMethod, string transactionId, bool isSuccessful, DateTime processedAt, string orderNumber);
        Task PublishUserRegisteredAsync(string userId, string email, string firstName, string lastName, DateTime registeredAt, string registrationSource);
        Task PublishPasswordResetRequestedAsync(string userId, string email, string resetToken, DateTime requestedAt, DateTime expiresAt);
        Task PublishEmailSendAsync(string toEmail, string toName, string subject, string htmlContent, string plainTextContent, string emailType, DateTime requestedAt, Guid? correlationId = null);
        Task PublishAdminNotificationAsync(string notificationType, string title, string message, string severity, DateTime createdAt, string source, object? additionalData = null);
        Task PublishSystemMetricsAlertAsync(string metricName, double currentValue, double thresholdValue, string severity, string endpoint, DateTime detectedAt, string message);
        Task PublishUserActivityAsync(string userId, string activity, string details, string ipAddress, string userAgent, DateTime timestamp, string? sessionId = null);
    }
}