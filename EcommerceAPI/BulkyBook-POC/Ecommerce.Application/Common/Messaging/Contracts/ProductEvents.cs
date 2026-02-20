using System;

namespace Ecommerce.Application.Common.Messaging.Contracts
{
    /// <summary>
    /// Event published when a product is created
    /// </summary>
    public interface IProductCreatedEvent
    {
        int ProductId { get; }
        string ProductName { get; }
        string Description { get; }
        decimal Price { get; }
        int CategoryId { get; }
        string CategoryName { get; }
        int StockQuantity { get; }
        DateTime CreatedAt { get; }
        string CreatedBy { get; }
    }

    /// <summary>
    /// Event published when product inventory changes
    /// </summary>
    public interface IProductInventoryChangedEvent
    {
        int ProductId { get; }
        string ProductName { get; }
        int OldQuantity { get; }
        int NewQuantity { get; }
        int ChangeAmount { get; }
        string ChangeReason { get; } // "order", "restock", "adjustment", etc.
        DateTime ChangedAt { get; }
        string ChangedBy { get; }
        Guid? OrderId { get; } // If change is due to an order
    }

    /// <summary>
    /// Event published when a product is updated
    /// </summary>
    public interface IProductUpdatedEvent
    {
        int ProductId { get; }
        string ProductName { get; }
        string[] ChangedFields { get; }
        DateTime UpdatedAt { get; }
        string UpdatedBy { get; }
    }
}





