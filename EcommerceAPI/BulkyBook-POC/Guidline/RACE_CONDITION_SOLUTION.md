# Race Condition Solution for E-Commerce Stock Management

## 🚨 **The Problem: Race Condition in Stock Management**

### **Scenario:**
- Product has **1 stock** remaining
- **2 users** try to purchase simultaneously
- Both users pass the stock check
- **Both orders get created** → **Stock goes negative!**

### **What Happens in Current Code:**
```csharp
// User A and User B both execute this simultaneously:
var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
if (product.Stock < item.Quantity) // Both see stock = 1, both pass this check
    throw new InvalidOperationException("Insufficient stock...");

product.Stock -= item.Quantity; // Both decrement: 1 - 1 = 0, then 0 - 1 = -1!
```

## ⚠️ **Why This Happens:**

1. **No Database Transactions** - Each operation is separate
2. **No Concurrency Control** - No locking mechanism  
3. **Read-Then-Write Race** - Time gap between check and update
4. **Stock Can Go Negative** - No database constraints preventing this

## ✅ **Solutions Implemented:**

### **1. Atomic Stock Reservation**
```csharp
// Use raw SQL with atomic update and stock check
var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
    @"UPDATE Products 
      SET Stock = Stock - {0} 
      WHERE ProductId = {1} 
        AND Stock >= {0} 
        AND IsDeleted = 0",
    quantity, productId);

return rowsAffected > 0; // True if stock was reserved, false if insufficient
```

### **2. Database-Level Concurrency Control**
```csharp
// Row-level locking prevents race conditions
var currentStock = await context.Database.SqlQueryRaw<int>(
    "SELECT Stock FROM Products WITH (UPDLOCK, ROWLOCK) WHERE ProductId = {0}",
    productId).FirstOrDefaultAsync();
```

### **3. Distributed Transactions**
```csharp
using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
try
{
    // All operations are atomic
    await ValidateAndReserveStockAsync(items);
    var order = await CreateOrderAsync(request);
    transaction.Complete(); // Commit all or rollback all
}
```

### **4. Batch Stock Reservation**
```csharp
// Reserve stock for multiple products atomically
var reservations = items.Select(item => new ProductReservation 
{ 
    ProductId = item.ProductId, 
    Quantity = item.Quantity 
}).ToList();

var success = await _context.TryReserveStockBatchAsync(reservations);
```

## 🔧 **Implementation Details:**

### **ConcurrencyControlExtensions.cs**
- `TryReserveStockAsync()` - Atomic stock reservation
- `TryReserveStockBatchAsync()` - Batch reservation with transaction
- `GetCurrentStockWithLockAsync()` - Row-level locking
- `HasSufficientStockAsync()` - Safe stock checking

### **CheckoutOrderHandlerConcurrent.cs**
- **Step 1:** Validate all products exist
- **Step 2:** Atomically reserve stock for all products
- **Step 3:** Create order (only if stock reserved)
- **Step 4:** Commit transaction
- **Step 5:** Publish events (outside transaction)
- **Step 6:** Invalidate caches

## 🧪 **Testing the Solution:**

### **Test Scenario:**
```bash
# Terminal 1: User A places order
curl -X POST "https://localhost:7273/api/orders/checkout" \
  -H "Authorization: Bearer tokenA" \
  -d '{"items":[{"productId":1,"quantity":1}]}'

# Terminal 2: User B places order simultaneously  
curl -X POST "https://localhost:7273/api/orders/checkout" \
  -H "Authorization: Bearer tokenB" \
  -d '{"items":[{"productId":1,"quantity":1}]}'
```

### **Expected Results:**
- ✅ **User A:** Order created successfully
- ❌ **User B:** "Insufficient stock" error
- ✅ **Stock:** Remains at 0 (not negative)

## 📊 **Performance Considerations:**

### **Database Locks:**
- **UPDLOCK, ROWLOCK** - Minimal lock scope
- **Short transaction duration** - Reduces lock contention
- **Batch operations** - Fewer round trips

### **Caching Strategy:**
- **Cache invalidation** after successful orders
- **No cache during checkout** - Always check database
- **Async cache updates** - Don't block order creation

## 🔄 **Integration with RabbitMQ:**

### **Event Publishing:**
```csharp
// Publish events AFTER transaction commits
await _messagePublisher.PublishOrderCreatedAsync(
    order.Id, userId, userEmail, totalAmount, DateTime.UtcNow, orderNumber);
```

### **Benefits:**
- **Reliable messaging** - Events only published on success
- **Async processing** - Email sending doesn't block orders
- **Event sourcing** - Full audit trail of all orders

## 🛡️ **Error Handling:**

### **Stock Insufficient:**
```csharp
throw new InvalidOperationException(
    $"Insufficient stock for {productName}. Available: {availableStock}, " +
    $"Requested: {requestedQuantity}. Another customer may have just " +
    $"purchased this item. Please refresh and try again.");
```

### **Product Not Found:**
```csharp
throw new InvalidOperationException(
    $"Product with ID {productId} not found or has been removed.");
```

### **Transaction Rollback:**
- **Automatic rollback** on any exception
- **No partial orders** - All or nothing
- **Clean error messages** - User-friendly feedback

## 📈 **Monitoring & Logging:**

### **Success Logging:**
```csharp
_logger.LogInformation("Order {OrderId} created successfully for user {UserId}", 
    order.Id, userId);
```

### **Error Logging:**
```csharp
_logger.LogError(ex, "Failed to create order for user {UserId}. Error: {ErrorMessage}", 
    userId, ex.Message);
```

### **Performance Metrics:**
- **Order creation time**
- **Stock reservation time**
- **Transaction duration**
- **Cache hit/miss rates**

## 🚀 **Deployment Strategy:**

### **1. Database Migration:**
```sql
-- Add check constraint to prevent negative stock
ALTER TABLE Products ADD CONSTRAINT CK_Products_Stock_Positive 
CHECK (Stock >= 0);

-- Add index for faster stock queries
CREATE INDEX IX_Products_Stock_ProductId ON Products (ProductId, Stock) 
WHERE IsDeleted = 0;
```

### **2. Configuration:**
```json
{
  "Database": {
    "CommandTimeout": 30,
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3
  }
}
```

### **3. Monitoring:**
- **Database performance** - Query execution times
- **Lock contention** - Monitor blocking sessions
- **Error rates** - Track failed orders
- **Stock accuracy** - Regular stock audits

## 🔮 **Future Enhancements:**

### **1. Optimistic Concurrency:**
```csharp
// Add version column to Products table
[Timestamp]
public byte[] RowVersion { get; set; }
```

### **2. Stock Reservation System:**
```csharp
// Reserve stock for 15 minutes before payment
public class StockReservation
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string SessionId { get; set; }
}
```

### **3. Distributed Locking:**
```csharp
// Use Redis for distributed locks in multi-instance scenarios
using var lock = await _distributedLock.AcquireAsync($"product_{productId}");
```

## 📚 **Best Practices:**

1. **Always use transactions** for multi-step operations
2. **Check stock at database level** - not in application
3. **Use row-level locking** for concurrency control
4. **Publish events after commit** - ensure consistency
5. **Handle errors gracefully** - provide clear feedback
6. **Monitor performance** - track lock contention
7. **Test race conditions** - simulate concurrent users
8. **Regular stock audits** - verify data integrity

## 🎯 **Summary:**

The implemented solution provides:
- ✅ **Race condition prevention** - Atomic stock operations
- ✅ **Data consistency** - Distributed transactions
- ✅ **Performance optimization** - Minimal locking
- ✅ **Error handling** - Clear user feedback
- ✅ **Monitoring** - Comprehensive logging
- ✅ **Scalability** - Works with multiple instances
- ✅ **Integration** - Seamless RabbitMQ integration

Your e-commerce system is now **race condition safe** and ready for production! 🚀





