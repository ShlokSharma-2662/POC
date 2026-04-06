# RabbitMQ Implementation Guide

## 🎯 Overview
Successfully implemented RabbitMQ with MassTransit for asynchronous message processing in the E-Commerce API project.

## 📦 Packages Added
- `MassTransit` - Core messaging framework
- `MassTransit.RabbitMQ` - RabbitMQ transport
- `MassTransit.Extensions.DependencyInjection` - DI integration

## ⚙️ Configuration

### appsettings.json
```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Enabled": true
  }
}
```

## 🏗️ Architecture

### Message Contracts
Created in `Ecommerce.Application/Common/Messaging/Contracts/`:
- **OrderEvents.cs** - Order lifecycle events
- **EmailEvents.cs** - Email-related events
- **ProductEvents.cs** - Product management events
- **AdminEvents.cs** - Admin notifications and system alerts

### Message Consumers
Implemented in `Ecommerce.Infrastructure/Messaging/Consumers/`:
- **EmailConsumer.cs** - Handles email sending, user registration, password reset
- **OrderConsumer.cs** - Handles order creation, status changes, payment processing
- **AdminConsumer.cs** - Handles admin notifications, system metrics, user activity

### Message Publisher Service
Created `MessagePublisherService` in `Ecommerce.Infrastructure/Services/` for easy message publishing.

## 🔄 Message Flow

### Order Processing Flow
1. **Order Created** → `IOrderCreatedEvent` → **OrderConsumer** → **Email Confirmation**
2. **Order Status Changed** → `IOrderStatusChangedEvent` → **OrderConsumer** → **Status Update Email**
3. **Payment Processed** → `IPaymentProcessedEvent` → **OrderConsumer** → **Payment Confirmation**

### User Registration Flow
1. **User Registered** → `IUserRegisteredEvent` → **UserRegisteredConsumer** → **Welcome Email**

### Admin Notifications
1. **System Alerts** → `ISystemMetricsAlertEvent` → **AdminConsumer** → **Admin Email**
2. **User Activity** → `IUserActivityEvent` → **AdminConsumer** → **Activity Logging**

## 🚀 Usage Examples

### Publishing Messages in Controllers
```csharp
// In OrdersController
await _messagePublisher.PublishOrderCreatedAsync(
    orderId, 
    userId, 
    userEmail, 
    totalAmount, 
    DateTime.UtcNow, 
    orderNumber);

// In AuthController
await _messagePublisher.PublishUserRegisteredAsync(
    userId,
    email,
    firstName,
    lastName,
    DateTime.UtcNow,
    "web");
```

### Publishing Admin Notifications
```csharp
await _messagePublisher.PublishAdminNotificationAsync(
    "high_latency",
    "API Performance Alert",
    "Response time exceeded 1000ms",
    "high",
    DateTime.UtcNow,
    "OrdersController");
```

## 🐳 RabbitMQ Setup

### Using Docker (Recommended)
```bash
# Run RabbitMQ with Management UI
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  -e RABBITMQ_DEFAULT_USER=guest \
  -e RABBITMQ_DEFAULT_PASS=guest \
  rabbitmq:3-management

# Access Management UI at http://localhost:15672
# Username: guest, Password: guest
```

### Manual Installation
1. Download RabbitMQ from https://www.rabbitmq.com/download.html
2. Install and start the service
3. Enable management plugin: `rabbitmq-plugins enable rabbitmq_management`

## 🧪 Testing

### 1. Start RabbitMQ Server
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### 2. Enable RabbitMQ in Configuration
Set `"RabbitMQ:Enabled": true` in appsettings.json

### 3. Test Order Creation
```bash
POST /api/orders/checkout
{
  "fullName": "John Doe",
  "address": "123 Main St",
  "phoneNumber": "555-1234",
  "items": [
    {
      "productId": 1,
      "quantity": 2,
      "price": 29.99
    }
  ]
}
```

### 4. Test User Registration
```bash
POST /api/auth/register
{
  "email": "test@example.com",
  "password": "Password123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

### 5. Monitor Messages
- Access RabbitMQ Management UI at http://localhost:15672
- Check Queues tab to see message processing
- View Exchanges and Bindings for message routing

## 🔧 Troubleshooting

### Common Issues

1. **Connection Failed**
   - Ensure RabbitMQ server is running
   - Check connection string in appsettings.json
   - Verify firewall settings

2. **Messages Not Processing**
   - Check if `RabbitMQ:Enabled` is set to `true`
   - Verify consumer registration in DI container
   - Check application logs for errors

3. **Email Not Sending**
   - Verify SendGrid configuration
   - Check email service implementation
   - Review consumer error logs

### Logging
All message publishing and consumption is logged. Check application logs for:
- Message publishing success/failure
- Consumer processing status
- Error details and stack traces

## 📈 Benefits

### Performance
- **Asynchronous Processing** - Non-blocking email sending
- **Scalability** - Multiple consumers can process messages
- **Reliability** - Message persistence and retry mechanisms

### Maintainability
- **Decoupled Architecture** - Controllers don't directly depend on email services
- **Event-Driven** - Easy to add new event handlers
- **Testability** - Messages can be mocked for unit tests

### Monitoring
- **Message Tracking** - Full visibility into message flow
- **Error Handling** - Failed messages can be retried or moved to dead letter queues
- **Metrics** - Message processing rates and performance

## 🔮 Future Enhancements

1. **Dead Letter Queues** - Handle failed messages
2. **Message Scheduling** - Delayed message processing
3. **Message Routing** - Advanced routing based on message content
4. **Monitoring Integration** - Prometheus/Grafana metrics
5. **Message Encryption** - Secure message transmission
6. **Clustering** - High availability setup

## 📚 Resources

- [MassTransit Documentation](https://masstransit.io/)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Message Queue Patterns](https://www.enterpriseintegrationpatterns.com/)





