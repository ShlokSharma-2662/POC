# Ecommerce API

A comprehensive e-commerce API built with .NET 9, featuring modern architecture patterns, authentication, payment processing, and real-time capabilities.

## 🚀 Features

- **Clean Architecture**: Domain-driven design with separation of concerns
- **Authentication & Authorization**: JWT-based authentication with OAuth support
- **Payment Processing**: Stripe integration for secure payments
- **Real-time Communication**: SignalR for live updates
- **Caching**: Redis integration for performance optimization
- **Message Queuing**: RabbitMQ for asynchronous processing
- **Rate Limiting**: API rate limiting for security
- **Comprehensive Testing**: 152 unit tests with 100% pass rate
- **API Documentation**: Swagger/OpenAPI documentation
- **Logging**: Structured logging with Serilog
- **Database**: Entity Framework Core with SQL Server

## 🏗️ Architecture

The project follows Clean Architecture principles with the following layers:

- **Ecommerce.API**: Web API layer with controllers and middleware
- **Ecommerce.Application**: Application logic with CQRS pattern using MediatR
- **Ecommerce.Domain**: Core business entities and interfaces
- **Ecommerce.Infrastructure**: Data access, external services, and infrastructure concerns
- **Ecommerce.Tests**: Comprehensive unit and integration tests

## 📋 Prerequisites

- .NET 9.0 SDK
- SQL Server (LocalDB or full instance)
- Redis (for caching)
- RabbitMQ (for message queuing)
- Stripe account (for payment processing)

## 🛠️ Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd EcommerceAPI/BulkyBook-POC
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Update database connection string**
   Edit `Ecommerce.API/appsettings.json` and `Ecommerce.API/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EcommerceDB;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

4. **Configure external services**
   Update the following settings in `appsettings.json`:
   ```json
   {
     "Stripe": {
       "PublishableKey": "your_stripe_publishable_key",
       "SecretKey": "your_stripe_secret_key"
     },
     "Redis": {
       "ConnectionString": "localhost:6379"
     },
     "RabbitMQ": {
       "HostName": "localhost",
       "Port": 5672,
       "UserName": "guest",
       "Password": "guest"
     }
   }
   ```

## 🚀 Getting Started

1. **Build the solution**
   ```bash
   dotnet build
   ```

2. **Run the database migrations**
   ```bash
   cd Ecommerce.API
   dotnet ef database update
   ```

3. **Run the API**
   ```bash
   dotnet run --project Ecommerce.API
   ```

4. **Access the API**
   - API: `https://localhost:7001`
   - Swagger UI: `https://localhost:7001/swagger`

## 🧪 Testing

Run all tests:
```bash
dotnet test
```

Run tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📚 API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh JWT token
- `POST /api/auth/logout` - User logout

### Products
- `GET /api/products` - Get all products (with pagination)
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create new product (Admin only)
- `PUT /api/products/{id}` - Update product (Admin only)
- `DELETE /api/products/{id}` - Delete product (Admin only)

### Categories
- `GET /api/categories` - Get all categories
- `POST /api/categories` - Create category (Admin only)

### Cart
- `GET /api/cart` - Get user's cart
- `POST /api/cart/add` - Add item to cart
- `PUT /api/cart/update` - Update cart item quantity
- `DELETE /api/cart/remove/{id}` - Remove item from cart

### Orders
- `GET /api/orders` - Get user's orders
- `POST /api/orders/checkout` - Create new order
- `GET /api/orders/{id}` - Get order details

### Payments
- `POST /api/payments/intent` - Create payment intent
- `POST /api/payments/confirm` - Confirm payment

### Wishlist
- `GET /api/wishlist` - Get user's wishlist
- `POST /api/wishlist/add` - Add item to wishlist
- `DELETE /api/wishlist/remove/{id}` - Remove item from wishlist

## 🔧 Configuration

### Environment Variables
- `ASPNETCORE_ENVIRONMENT`: Set to `Development`, `Staging`, or `Production`
- `ConnectionStrings__DefaultConnection`: Database connection string
- `Stripe__SecretKey`: Stripe secret key
- `Redis__ConnectionString`: Redis connection string

### Rate Limiting
The API implements rate limiting to prevent abuse:
- 100 requests per minute per IP address
- 1000 requests per hour per authenticated user

## 🚀 Deployment

### Docker
```bash
docker build -t ecommerce-api .
docker run -p 5000:80 ecommerce-api
```

### Azure
1. Create an Azure App Service
2. Configure connection strings and app settings
3. Deploy using Azure DevOps or GitHub Actions

## 📊 Monitoring

The API includes comprehensive monitoring:
- **Application Insights**: Performance and error tracking
- **Serilog**: Structured logging
- **Health Checks**: API health monitoring
- **Metrics**: Custom performance metrics

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

For support, email support@ecommerce-api.com or create an issue in the repository.

## 🔄 Version History

- **v1.0.0** - Initial release with core e-commerce functionality
- **v1.1.0** - Added OAuth authentication and rate limiting
- **v1.2.0** - Implemented Redis caching and RabbitMQ messaging
- **v1.3.0** - Added comprehensive testing and monitoring