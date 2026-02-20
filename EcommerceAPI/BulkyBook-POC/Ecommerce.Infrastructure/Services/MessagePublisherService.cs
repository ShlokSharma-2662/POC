using Ecommerce.Domain.Common.Messaging.Contracts;
using Ecommerce.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services
{
    public class MessagePublisherService : IMessagePublisherService
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<MessagePublisherService> _logger;

        public MessagePublisherService(IPublishEndpoint publishEndpoint, ILogger<MessagePublisherService> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task PublishOrderCreatedAsync(Guid orderId, string userId, string userEmail, decimal totalAmount, DateTime createdAt, string orderNumber)
        {
            try
            {
                var message = new OrderCreatedEvent
                {
                    OrderId = orderId,
                    UserId = userId,
                    UserEmail = userEmail,
                    TotalAmount = totalAmount,
                    CreatedAt = createdAt,
                    OrderNumber = orderNumber
                };

                await _publishEndpoint.Publish<IOrderCreatedEvent>(message);
                _logger.LogInformation("Published OrderCreated event for Order {OrderNumber}", orderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreated event for Order {OrderNumber}", orderNumber);
                throw;
            }
        }

        public async Task PublishOrderStatusChangedAsync(Guid orderId, string userId, string userEmail, string oldStatus, string newStatus, DateTime changedAt, string orderNumber)
        {
            try
            {
                var message = new OrderStatusChangedEvent
                {
                    OrderId = orderId,
                    UserId = userId,
                    UserEmail = userEmail,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedAt = changedAt,
                    OrderNumber = orderNumber
                };

                await _publishEndpoint.Publish<IOrderStatusChangedEvent>(message);
                _logger.LogInformation("Published OrderStatusChanged event for Order {OrderNumber} from {OldStatus} to {NewStatus}", 
                    orderNumber, oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderStatusChanged event for Order {OrderNumber}", orderNumber);
                throw;
            }
        }

        public async Task PublishPaymentProcessedAsync(Guid orderId, string userId, string userEmail, decimal amount, string paymentMethod, string transactionId, bool isSuccessful, DateTime processedAt, string orderNumber)
        {
            try
            {
                var message = new PaymentProcessedEvent
                {
                    OrderId = orderId,
                    UserId = userId,
                    UserEmail = userEmail,
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    TransactionId = transactionId,
                    IsSuccessful = isSuccessful,
                    ProcessedAt = processedAt,
                    OrderNumber = orderNumber
                };

                await _publishEndpoint.Publish<IPaymentProcessedEvent>(message);
                _logger.LogInformation("Published PaymentProcessed event for Order {OrderNumber}, Success: {IsSuccessful}", 
                    orderNumber, isSuccessful);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PaymentProcessed event for Order {OrderNumber}", orderNumber);
                throw;
            }
        }

        public async Task PublishUserRegisteredAsync(string userId, string email, string firstName, string lastName, DateTime registeredAt, string registrationSource)
        {
            try
            {
                var message = new UserRegisteredEvent
                {
                    UserId = userId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    RegisteredAt = registeredAt,
                    RegistrationSource = registrationSource
                };

                await _publishEndpoint.Publish<IUserRegisteredEvent>(message);
                _logger.LogInformation("Published UserRegistered event for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish UserRegistered event for {Email}", email);
                throw;
            }
        }

        public async Task PublishPasswordResetRequestedAsync(string userId, string email, string resetToken, DateTime requestedAt, DateTime expiresAt)
        {
            try
            {
                var message = new PasswordResetRequestedEvent
                {
                    UserId = userId,
                    Email = email,
                    ResetToken = resetToken,
                    RequestedAt = requestedAt,
                    ExpiresAt = expiresAt
                };

                await _publishEndpoint.Publish<IPasswordResetRequestedEvent>(message);
                _logger.LogInformation("Published PasswordResetRequested event for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PasswordResetRequested event for {Email}", email);
                throw;
            }
        }

        public async Task PublishEmailSendAsync(string toEmail, string toName, string subject, string htmlContent, string plainTextContent, string emailType, DateTime requestedAt, Guid? correlationId = null)
        {
            try
            {
                var message = new EmailSendEvent
                {
                    ToEmail = toEmail,
                    ToName = toName,
                    Subject = subject,
                    HtmlContent = htmlContent,
                    PlainTextContent = plainTextContent,
                    EmailType = emailType,
                    RequestedAt = requestedAt,
                    CorrelationId = correlationId
                };

                await _publishEndpoint.Publish<IEmailSendEvent>(message);
                _logger.LogInformation("Published EmailSend event for {EmailType} to {ToEmail}", emailType, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish EmailSend event for {EmailType} to {ToEmail}", emailType, toEmail);
                throw;
            }
        }

        public async Task PublishAdminNotificationAsync(string notificationType, string title, string message, string severity, DateTime createdAt, string source, object? additionalData = null)
        {
            try
            {
                var notificationMessage = new AdminNotificationEvent
                {
                    NotificationType = notificationType,
                    Title = title,
                    Message = message,
                    Severity = severity,
                    CreatedAt = createdAt,
                    Source = source,
                    AdditionalData = additionalData
                };

                await _publishEndpoint.Publish<IAdminNotificationEvent>(notificationMessage);
                _logger.LogInformation("Published AdminNotification event for {NotificationType}", notificationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AdminNotification event for {NotificationType}", notificationType);
                throw;
            }
        }

        public async Task PublishSystemMetricsAlertAsync(string metricName, double currentValue, double thresholdValue, string severity, string endpoint, DateTime detectedAt, string message)
        {
            try
            {
                var alertMessage = new SystemMetricsAlertEvent
                {
                    MetricName = metricName,
                    CurrentValue = currentValue,
                    ThresholdValue = thresholdValue,
                    Severity = severity,
                    Endpoint = endpoint,
                    DetectedAt = detectedAt,
                    Message = message
                };

                await _publishEndpoint.Publish<ISystemMetricsAlertEvent>(alertMessage);
                _logger.LogInformation("Published SystemMetricsAlert event for {MetricName}", metricName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish SystemMetricsAlert event for {MetricName}", metricName);
                throw;
            }
        }

        public async Task PublishUserActivityAsync(string userId, string activity, string details, string ipAddress, string userAgent, DateTime timestamp, string? sessionId = null)
        {
            try
            {
                var message = new UserActivityEvent
                {
                    UserId = userId,
                    Activity = activity,
                    Details = details,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Timestamp = timestamp,
                    SessionId = sessionId
                };

                await _publishEndpoint.Publish<IUserActivityEvent>(message);
                _logger.LogInformation("Published UserActivity event for {UserId} - {Activity}", userId, activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish UserActivity event for {UserId}", userId);
                throw;
            }
        }
    }

    // Message implementations
    public class OrderCreatedEvent : IOrderCreatedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
    }

    public class OrderStatusChangedEvent : IOrderStatusChangedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
    }

    public class PaymentProcessedEvent : IPaymentProcessedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
    }

    public class UserRegisteredEvent : IUserRegisteredEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public string RegistrationSource { get; set; } = string.Empty;
    }

    public class PasswordResetRequestedEvent : IPasswordResetRequestedEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class EmailSendEvent : IEmailSendEvent
    {
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string PlainTextContent { get; set; } = string.Empty;
        public string EmailType { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public Guid? CorrelationId { get; set; }
    }

    public class AdminNotificationEvent : IAdminNotificationEvent
    {
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = string.Empty;
        public object? AdditionalData { get; set; }
    }

    public class SystemMetricsAlertEvent : ISystemMetricsAlertEvent
    {
        public string MetricName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double ThresholdValue { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UserActivityEvent : IUserActivityEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? SessionId { get; set; }
    }
}
