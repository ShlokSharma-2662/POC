using System;

namespace Ecommerce.Domain.Common.Messaging.Contracts
{
    /// <summary>
    /// Event published when a new order is created
    /// </summary>
    public interface IOrderCreatedEvent
    {
        Guid OrderId { get; }
        string UserId { get; }
        string UserEmail { get; }
        decimal TotalAmount { get; }
        DateTime CreatedAt { get; }
        string OrderNumber { get; }
    }

    /// <summary>
    /// Event published when an order status changes
    /// </summary>
    public interface IOrderStatusChangedEvent
    {
        Guid OrderId { get; }
        string UserId { get; }
        string UserEmail { get; }
        string OldStatus { get; }
        string NewStatus { get; }
        DateTime ChangedAt { get; }
        string OrderNumber { get; }
    }

    /// <summary>
    /// Event published when payment is processed
    /// </summary>
    public interface IPaymentProcessedEvent
    {
        Guid OrderId { get; }
        string UserId { get; }
        string UserEmail { get; }
        decimal Amount { get; }
        string PaymentMethod { get; }
        string TransactionId { get; }
        bool IsSuccessful { get; }
        DateTime ProcessedAt { get; }
        string OrderNumber { get; }
    }
}
