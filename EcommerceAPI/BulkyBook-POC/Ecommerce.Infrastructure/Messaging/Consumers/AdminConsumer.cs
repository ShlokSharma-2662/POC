using Ecommerce.Domain.Common.Messaging.Contracts;
using Ecommerce.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Consumer for handling admin notification events
    /// </summary>
    public class AdminNotificationConsumer : IConsumer<IAdminNotificationEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminNotificationConsumer> _logger;

        public AdminNotificationConsumer(IEmailService emailService, ILogger<AdminNotificationConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IAdminNotificationEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("Processing admin notification event: {NotificationType} - {Severity}", 
                    message.NotificationType, message.Severity);

                // Only send email for high/critical severity notifications
                if (message.Severity.ToLower() is "high" or "critical")
                {
                    var subject = $"[{message.Severity.ToUpper()}] {message.Title}";
                    var htmlContent = $@"
                        <h2>{message.Title}</h2>
                        <p><strong>Type:</strong> {message.NotificationType}</p>
                        <p><strong>Severity:</strong> {message.Severity}</p>
                        <p><strong>Source:</strong> {message.Source}</p>
                        <p><strong>Time:</strong> {message.CreatedAt:yyyy-MM-dd HH:mm:ss}</p>
                        <p><strong>Message:</strong></p>
                        <p>{message.Message}</p>";

                    var plainTextContent = $"{message.Title}. Type: {message.NotificationType}, Severity: {message.Severity}, Source: {message.Source}, Time: {message.CreatedAt:yyyy-MM-dd HH:mm:ss}. Message: {message.Message}";

                    // Send to admin email (you might want to make this configurable)
                    await _emailService.SendEmailAsync(
                        "admin@yourapp.com", // Configure this in appsettings
                        "Admin",
                        subject,
                        htmlContent,
                        plainTextContent);

                    _logger.LogInformation("Successfully sent admin notification email for {NotificationType}", message.NotificationType);
                }
                else
                {
                    _logger.LogInformation("Admin notification {NotificationType} with severity {Severity} logged but not emailed", 
                        message.NotificationType, message.Severity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send admin notification email for {NotificationType}. Error: {Error}", 
                    message.NotificationType, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Consumer for handling system metrics alert events
    /// </summary>
    public class SystemMetricsAlertConsumer : IConsumer<ISystemMetricsAlertEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<SystemMetricsAlertConsumer> _logger;

        public SystemMetricsAlertConsumer(IEmailService emailService, ILogger<SystemMetricsAlertConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ISystemMetricsAlertEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("Processing system metrics alert: {MetricName} - {Severity}", 
                    message.MetricName, message.Severity);

                // Only send email for high/critical severity alerts
                if (message.Severity.ToLower() is "high" or "critical")
                {
                    var subject = $"[{message.Severity.ToUpper()}] System Alert - {message.MetricName}";
                    var htmlContent = $@"
                        <h2>System Performance Alert</h2>
                        <p><strong>Metric:</strong> {message.MetricName}</p>
                        <p><strong>Current Value:</strong> {message.CurrentValue}</p>
                        <p><strong>Threshold:</strong> {message.ThresholdValue}</p>
                        <p><strong>Severity:</strong> {message.Severity}</p>
                        <p><strong>Endpoint:</strong> {message.Endpoint}</p>
                        <p><strong>Detected:</strong> {message.DetectedAt:yyyy-MM-dd HH:mm:ss}</p>
                        <p><strong>Message:</strong></p>
                        <p>{message.Message}</p>
                        <p>Please investigate this issue immediately.</p>";

                    var plainTextContent = $"System Performance Alert. Metric: {message.MetricName}, Current Value: {message.CurrentValue}, Threshold: {message.ThresholdValue}, Severity: {message.Severity}, Endpoint: {message.Endpoint}, Detected: {message.DetectedAt:yyyy-MM-dd HH:mm:ss}. Message: {message.Message}. Please investigate this issue immediately.";

                    await _emailService.SendEmailAsync(
                        "admin@yourapp.com", // Configure this in appsettings
                        "Admin",
                        subject,
                        htmlContent,
                        plainTextContent);

                    _logger.LogInformation("Successfully sent system metrics alert email for {MetricName}", message.MetricName);
                }
                else
                {
                    _logger.LogInformation("System metrics alert {MetricName} with severity {Severity} logged but not emailed", 
                        message.MetricName, message.Severity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system metrics alert email for {MetricName}. Error: {Error}", 
                    message.MetricName, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Consumer for handling user activity events (for logging/auditing)
    /// </summary>
    public class UserActivityConsumer : IConsumer<IUserActivityEvent>
    {
        private readonly ILogger<UserActivityConsumer> _logger;

        public UserActivityConsumer(ILogger<UserActivityConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IUserActivityEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("User Activity: {UserId} - {Activity} - {Details} - {IpAddress} - {Timestamp}", 
                    message.UserId, message.Activity, message.Details, message.IpAddress, message.Timestamp);

                // Here you could save to a database, send to analytics service, etc.
                // For now, we're just logging it
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process user activity event for {UserId}. Error: {Error}", 
                    message.UserId, ex.Message);
                throw;
            }
        }
    }
}
