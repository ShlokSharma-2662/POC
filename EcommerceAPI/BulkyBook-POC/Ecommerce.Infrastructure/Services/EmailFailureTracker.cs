using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// Tracks email failures for alerting purposes
    /// </summary>
    public interface IEmailFailureTracker
    {
        void RecordFailure(string emailType, string recipient);
        void RecordSuccess(string emailType, string recipient);
        bool ShouldAlert(string emailType);
        int GetFailureCount(string emailType, TimeSpan timeWindow);
        void ResetFailures(string emailType);
    }

    public class EmailFailureTracker : IEmailFailureTracker
    {
        private readonly ILogger<EmailFailureTracker> _logger;
        private readonly ConcurrentDictionary<string, List<DateTime>> _failureHistory = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastAlertTime = new();
        
        // Configuration
        private readonly int _alertThreshold = 5; // Alert after 5 failures
        private readonly TimeSpan _alertCooldown = TimeSpan.FromMinutes(30); // Don't alert more than once per 30 minutes
        private readonly TimeSpan _failureWindow = TimeSpan.FromMinutes(10); // Count failures in last 10 minutes

        public EmailFailureTracker(ILogger<EmailFailureTracker> logger)
        {
            _logger = logger;
        }

        public void RecordFailure(string emailType, string recipient)
        {
            var key = $"{emailType}:{recipient}";
            var failures = _failureHistory.GetOrAdd(key, _ => new List<DateTime>());
            
            lock (failures)
            {
                failures.Add(DateTime.UtcNow);
                // Keep only failures within the time window
                var cutoff = DateTime.UtcNow - _failureWindow;
                failures.RemoveAll(f => f < cutoff);
            }

            var failureCount = GetFailureCount(emailType, _failureWindow);
            _logger.LogWarning(
                "Email failure recorded. Type: {EmailType}, Recipient: {Recipient}, Recent Failures: {FailureCount}",
                emailType, recipient, failureCount);

            // Check if we should alert
            if (ShouldAlert(emailType))
            {
                Alert(emailType, failureCount);
            }
        }

        public void RecordSuccess(string emailType, string recipient)
        {
            var key = $"{emailType}:{recipient}";
            if (_failureHistory.TryGetValue(key, out var failures))
            {
                lock (failures)
                {
                    failures.Clear();
                }
            }
        }

        public bool ShouldAlert(string emailType)
        {
            var failureCount = GetFailureCount(emailType, _failureWindow);
            
            if (failureCount < _alertThreshold)
                return false;

            // Check cooldown period
            if (_lastAlertTime.TryGetValue(emailType, out var lastAlert))
            {
                if (DateTime.UtcNow - lastAlert < _alertCooldown)
                    return false;
            }

            return true;
        }

        public int GetFailureCount(string emailType, TimeSpan timeWindow)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            var count = 0;

            foreach (var kvp in _failureHistory)
            {
                if (kvp.Key.StartsWith($"{emailType}:", StringComparison.OrdinalIgnoreCase))
                {
                    lock (kvp.Value)
                    {
                        count += kvp.Value.Count(f => f >= cutoff);
                    }
                }
            }

            return count;
        }

        public void ResetFailures(string emailType)
        {
            var keysToRemove = new List<string>();
            foreach (var key in _failureHistory.Keys)
            {
                if (key.StartsWith($"{emailType}:", StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _failureHistory.TryRemove(key, out _);
            }

            _lastAlertTime.TryRemove(emailType, out _);
        }

        private void Alert(string emailType, int failureCount)
        {
            _lastAlertTime[emailType] = DateTime.UtcNow;

            _logger.LogError(
                "🚨 EMAIL SERVICE ALERT: {FailureCount} failures detected for email type '{EmailType}' in the last {WindowMinutes} minutes. " +
                "Please investigate the email service configuration and connectivity.",
                failureCount, emailType, _failureWindow.TotalMinutes);

            // TODO: Integrate with alerting system (e.g., Application Insights, email to admins, etc.)
            // For now, we just log the alert
        }
    }
}

