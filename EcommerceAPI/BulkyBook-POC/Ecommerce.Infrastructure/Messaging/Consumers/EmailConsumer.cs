using Ecommerce.Domain.Common.Messaging.Contracts;
using Ecommerce.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Consumer for handling email sending events
    /// </summary>
    public class EmailConsumer : IConsumer<IEmailSendEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailConsumer> _logger;

        public EmailConsumer(IEmailService emailService, ILogger<EmailConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IEmailSendEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("Processing email send event for {EmailType} to {ToEmail}", 
                    message.EmailType, message.ToEmail);

                await _emailService.SendEmailAsync(
                    message.ToEmail,
                    message.ToName,
                    message.Subject,
                    message.HtmlContent,
                    message.PlainTextContent);

                _logger.LogInformation("Successfully sent {EmailType} email to {ToEmail}", 
                    message.EmailType, message.ToEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send {EmailType} email to {ToEmail}. Error: {Error}", 
                    message.EmailType, message.ToEmail, ex.Message);
                
                // Re-throw to trigger retry mechanism
                throw;
            }
        }
    }

    /// <summary>
    /// Consumer for handling user registration events
    /// </summary>
    public class UserRegisteredConsumer : IConsumer<IUserRegisteredEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<UserRegisteredConsumer> _logger;

        public UserRegisteredConsumer(IEmailService emailService, ILogger<UserRegisteredConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IUserRegisteredEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("Processing user registration event for {Email}", message.Email);

                // Send welcome email
                var subject = "Welcome to E-Commerce Store!";
                var htmlContent = $@"
                    <h2>Welcome {message.FirstName}!</h2>
                    <p>Thank you for registering with our E-Commerce Store.</p>
                    <p>Your account has been successfully created.</p>
                    <p>Best regards,<br/>The E-Commerce Team</p>";

                var plainTextContent = $"Welcome {message.FirstName}! Thank you for registering with our E-Commerce Store. Your account has been successfully created. Best regards, The E-Commerce Team";

                await _emailService.SendEmailAsync(
                    message.Email,
                    $"{message.FirstName} {message.LastName}",
                    subject,
                    htmlContent,
                    plainTextContent);

                _logger.LogInformation("Successfully sent welcome email to {Email}", message.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}. Error: {Error}", 
                    message.Email, ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Consumer for handling password reset events
    /// </summary>
    public class PasswordResetConsumer : IConsumer<IPasswordResetRequestedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetConsumer> _logger;

        public PasswordResetConsumer(IEmailService emailService, ILogger<PasswordResetConsumer> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IPasswordResetRequestedEvent> context)
        {
            var message = context.Message;
            
            try
            {
                _logger.LogInformation("Processing password reset event for {Email}", message.Email);

                var subject = "Password Reset Request";
                var resetUrl = $"https://yourapp.com/reset-password?token={message.ResetToken}";
                
                var htmlContent = $@"
                    <h2>Password Reset Request</h2>
                    <p>You have requested to reset your password.</p>
                    <p>Click the link below to reset your password:</p>
                    <p><a href=""{resetUrl}"">Reset Password</a></p>
                    <p>This link will expire on {message.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC.</p>
                    <p>If you didn't request this, please ignore this email.</p>";

                var plainTextContent = $"Password Reset Request. You have requested to reset your password. Click this link to reset: {resetUrl}. This link expires on {message.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC. If you didn't request this, please ignore this email.";

                await _emailService.SendEmailAsync(
                    message.Email,
                    "User",
                    subject,
                    htmlContent,
                    plainTextContent);

                _logger.LogInformation("Successfully sent password reset email to {Email}", message.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}. Error: {Error}", 
                    message.Email, ex.Message);
                throw;
            }
        }
    }
}
