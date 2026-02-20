using System;

namespace Ecommerce.Domain.Common.Messaging.Contracts
{
    /// <summary>
    /// Event published when admin needs to be notified
    /// </summary>
    public interface IAdminNotificationEvent
    {
        string NotificationType { get; } // "high_latency", "error_rate", "low_inventory", etc.
        string Title { get; }
        string Message { get; }
        string Severity { get; } // "low", "medium", "high", "critical"
        DateTime CreatedAt { get; }
        string Source { get; } // Which service/component triggered this
        object? AdditionalData { get; } // Additional context data
    }

    /// <summary>
    /// Event published when system metrics exceed thresholds
    /// </summary>
    public interface ISystemMetricsAlertEvent
    {
        string MetricName { get; }
        double CurrentValue { get; }
        double ThresholdValue { get; }
        string Severity { get; }
        string Endpoint { get; }
        DateTime DetectedAt { get; }
        string Message { get; }
    }

    /// <summary>
    /// Event published when user activity needs to be logged
    /// </summary>
    public interface IUserActivityEvent
    {
        string UserId { get; }
        string Activity { get; } // "login", "logout", "order_created", "product_viewed", etc.
        string Details { get; }
        string IpAddress { get; }
        string UserAgent { get; }
        DateTime Timestamp { get; }
        string? SessionId { get; }
    }
}
