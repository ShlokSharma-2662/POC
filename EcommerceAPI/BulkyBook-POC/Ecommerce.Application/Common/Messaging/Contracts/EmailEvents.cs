using System;

namespace Ecommerce.Application.Common.Messaging.Contracts
{
    /// <summary>
    /// Event published when an email needs to be sent
    /// </summary>
    public interface IEmailSendEvent
    {
        string ToEmail { get; }
        string ToName { get; }
        string Subject { get; }
        string HtmlContent { get; }
        string PlainTextContent { get; }
        string EmailType { get; } // "welcome", "order_confirmation", "password_reset", etc.
        DateTime RequestedAt { get; }
        Guid? CorrelationId { get; } // For tracking related events
    }

    /// <summary>
    /// Event published when user registration is completed
    /// </summary>
    public interface IUserRegisteredEvent
    {
        string UserId { get; }
        string Email { get; }
        string FirstName { get; }
        string LastName { get; }
        DateTime RegisteredAt { get; }
        string RegistrationSource { get; } // "web", "oauth", etc.
    }

    /// <summary>
    /// Event published when password reset is requested
    /// </summary>
    public interface IPasswordResetRequestedEvent
    {
        string UserId { get; }
        string Email { get; }
        string ResetToken { get; }
        DateTime RequestedAt { get; }
        DateTime ExpiresAt { get; }
    }
}





