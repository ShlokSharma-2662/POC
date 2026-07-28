# Dependency Injection Service

This directory contains the dependency injection configuration for the Ecommerce API. The `ServiceCollectionExtensions` class provides a clean and organized way to register all services used throughout the application.

## Overview

The dependency injection service is organized into several extension methods, each responsible for registering a specific category of services:

### Service Categories

1. **Infrastructure Services** (`AddInfrastructureServices`)
   - Database context (AppDbContext)
   - Application services (UserContextService)
   - Infrastructure services (JwtTokenGenerator, EmailService)

2. **Application Services** (`AddApplicationServices`)
   - HttpContextAccessor for UserContextService

3. **API Services** (`AddApiServices`)
   - Controllers with JSON configuration
   - API Explorer and OpenAPI

4. **Authentication Services** (`AddAuthenticationServices`)
   - JWT Bearer authentication
   - Token validation parameters

5. **CORS Services** (`AddCorsServices`)
   - Cross-Origin Resource Sharing configuration

6. **Logging Services** (`AddLoggingServices`)
   - Serilog configuration
   - Logging middleware

7. **Third-Party Services** (`AddThirdPartyServices`)
   - Stripe configuration
   - Application Insights telemetry

## Usage

### In Program.cs

Instead of registering services individually in `Program.cs`, you can now use the comprehensive registration method:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register all services using the dependency injection service
builder.Services.AddEcommerceServices(builder.Configuration);

// Add MediatR (if needed)
builder.Services.AddMediatR(Assembly.Load("Ecommerce.Application"));

// Add Swagger services (API-specific configuration)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce API", Version = "v1" });
    // JWT Bearer auth configuration...
});
```

### Individual Service Registration

If you need to register services individually, you can use the specific extension methods:

```csharp
// Register only infrastructure services
builder.Services.AddInfrastructureServices(configuration);

// Register only authentication services
builder.Services.AddAuthenticationServices(configuration);

// Register only CORS services
builder.Services.AddCorsServices(configuration);
```

## Benefits

1. **Clean Program.cs**: Reduces clutter in the main application entry point
2. **Modularity**: Services are organized by category and can be registered independently
3. **Maintainability**: Easy to add, remove, or modify service registrations
4. **Testability**: Individual service categories can be tested in isolation
5. **Reusability**: Service registration logic can be reused across different projects

## Adding New Services

To add new services to the dependency injection system:

1. **For Infrastructure Services**: Add to `AddInfrastructureServices` method
2. **For Application Services**: Add to `AddApplicationServices` method
3. **For API Services**: Add to `AddApiServices` method
4. **For Authentication**: Add to `AddAuthenticationServices` method
5. **For CORS**: Add to `AddCorsServices` method
6. **For Logging**: Add to `AddLoggingServices` method
7. **For Third-Party**: Add to `AddThirdPartyServices` method

Or create a new extension method for a new category of services.

## Configuration

The service registration methods accept an `IConfiguration` parameter, allowing you to read configuration values from `appsettings.json` or other configuration sources.

## Dependencies

This service depends on the following packages:
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- Microsoft.EntityFrameworkCore
- Microsoft.AspNetCore.Authentication.JwtBearer
- Serilog
- Stripe.net

Make sure these packages are installed in the Infrastructure project.

**Note**: Swagger/OpenAPI configuration is handled in the API project since it's specific to the web API layer.
