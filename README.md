# E-Commerce POC

Enterprise-style full-stack e-commerce proof of concept with separate backend and frontend applications.

- Backend API: `EcommerceAPI/BulkyBook-POC`
- Frontend UI: `ecommerce-ui`

This repository includes implementation docs for OAuth, Redis, rate limiting, messaging, pagination, email, and monitoring.

## 1. Project Overview

This project demonstrates:
- User registration/login with JWT
- OAuth login flows (Microsoft + Google)
- Product/category/cart/wishlist/order workflows
- Admin dashboards and operational endpoints
- Payment integration (Stripe)
- Email integration (SendGrid)
- Caching (Redis), messaging (RabbitMQ/MassTransit), resilience patterns (Polly)
- Structured logging and exception middleware

## 2. Repository Structure

```text
POC/
├─ EcommerceAPI/
│  └─ BulkyBook-POC/
│     ├─ Ecommerce.API/             # ASP.NET Core host (controllers, middleware, appsettings)
│     ├─ Ecommerce.Application/     # CQRS handlers, DTOs, business workflows
│     ├─ Ecommerce.Domain/          # Domain entities, interfaces
│     ├─ Ecommerce.Infrastructure/  # EF Core, service integrations, DI
│     ├─ Ecommerce.Tests/           # xUnit test project
│     ├─ scripts/                   # Build/deploy/test utility scripts
│     └─ Guidline/                  # Implementation and operations docs
├─ ecommerce-ui/
│  ├─ src/app/                      # Angular standalone components/services/guards/interceptors
│  ├─ src/environments/             # env configs
│  └─ dist/                         # production build output
├─ Images/
└─ root docs (*.md)
```

## 3. Tech Stack

### Backend
- .NET 9 / ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- MediatR (CQRS)
- Serilog + Application Insights
- JWT authentication + OAuth integrations
- AspNetCoreRateLimit + custom rate-limiting middleware
- Redis cache abstraction
- MassTransit + RabbitMQ
- Stripe, SendGrid
- Polly (retry/circuit breaker)

### Frontend
- Angular 19 (standalone components)
- RxJS
- Bootstrap 5 + SCSS
- Chart.js / ng2-charts
- ngx-toastr
- Stripe JS

### Testing
- xUnit, Moq, FluentAssertions, AutoFixture (backend)
- Karma/Jasmine (frontend)

## 4. Prerequisites

- Windows + PowerShell (commands below assume PowerShell)
- .NET SDK 9.x
- Node.js 20+ and npm
- SQL Server or LocalDB
- Optional services for full feature run:
  - Redis
  - RabbitMQ

## 5. Local Setup

## 5.1 Clone and open

```powershell
cd D:\POC_NEW\POC
```

## 5.2 Backend setup

```powershell
cd EcommerceAPI\BulkyBook-POC
dotnet restore
dotnet build
```

Configure backend values in:
- `EcommerceAPI/BulkyBook-POC/Ecommerce.API/appsettings.json`
- `EcommerceAPI/BulkyBook-POC/Ecommerce.API/appsettings.Development.json`

Recommended: keep secrets out of appsettings and use User Secrets / Key Vault.

## 5.3 Frontend setup

```powershell
cd ..\..\ecommerce-ui
npm install
```

Configure frontend env files:
- `ecommerce-ui/src/environments/environment.ts`
- `ecommerce-ui/src/environments/environment.prod.ts`

Set `apiUrl` to your running backend host.

## 6. Running the Applications

## 6.1 Run backend

```powershell
cd EcommerceAPI\BulkyBook-POC
dotnet run --project Ecommerce.API
```

Typical URLs:
- API: `https://localhost:7273`
- Swagger: `https://localhost:7273/swagger`

## 6.2 Run frontend

```powershell
cd ecommerce-ui
npm start
```

Typical URL:
- UI: `http://localhost:4200`

## 7. Build and Test

## 7.1 Backend tests

```powershell
cd EcommerceAPI\BulkyBook-POC
dotnet test Ecommerce.sln -c Debug
```

## 7.2 Backend coverage

```powershell
cd EcommerceAPI\BulkyBook-POC
dotnet test --collect:"XPlat Code Coverage"
```

## 7.3 Frontend tests

```powershell
cd ecommerce-ui
npm test
```

## 7.4 Frontend production build

```powershell
cd ecommerce-ui
npm run build
```

Output:
- `ecommerce-ui/dist/ecommerce-ui`

## 8. Configuration Reference

Key backend sections in appsettings:
- `ConnectionStrings:DefaultConnection`
- `Jwt:*`
- `OAuth:*`
- `GoogleOAuth:*`
- `Stripe:*`
- `SendGrid:*`
- `Redis:*`
- `RabbitMQ:*`
- `RateLimiting:*`
- `KeyVault:*`
- `Serilog:*`

Key frontend sections:
- `apiUrl`
- `stripePublishableKey`
- `oauth.*`
- `rateLimiting.*`

## 9. Security and Secrets

Do not commit real secrets in source-controlled config.

Use:
- .NET User Secrets for local development
- Azure Key Vault for production
- Environment variables for deployment pipelines

Relevant docs:
- `WHERE_TO_STORE_SECRETS.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/SECRETS_MANAGEMENT_GUIDE.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/SECRETS_MIGRATION_CHECKLIST.md`

## 10. Operational and Feature Docs

Core docs:
- `E-Commerce_Project_Presentation.md`
- `API_RESPONSE_REFACTORING_SUMMARY.md`
- `UI_RESPONSE_REFACTORING_SUMMARY.md`
- `GOOGLE_OAUTH_IMPLEMENTATION.md`
- `GUEST_USER_IMPLEMENTATION.md`
- `REDIS_COMPREHENSIVE_IMPLEMENTATION.md`
- `REDIS_IMPLEMENTATION_SUMMARY.md`
- `EMAIL_IMPLEMENTATION_SUMMARY.md`
- `EMAIL_DIAGNOSTIC_REPORT.md`
- `SENDGRID_SETUP.md`

Backend guideline folder:
- `EcommerceAPI/BulkyBook-POC/Guidline/README.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/HOW_TO_RUN_TESTS.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/RATE_LIMITING_TEST_GUIDE.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/RABBITMQ_IMPLEMENTATION_GUIDE.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/RESILIENCE_TESTING_GUIDE.md`

## 11. Common Troubleshooting

## 11.1 Backend startup fails
- Validate DB connection string.
- Confirm SQL Server/LocalDB is reachable.
- Check `Jwt:SecretKey` and other required settings.
- Review logs in console and Serilog sink outputs.

## 11.2 OAuth callback issues
- Verify callback URLs match exactly with provider config.
- Confirm frontend and backend base URLs in appsettings/env files.
- Ensure session/cookie and HTTPS settings are consistent across environments.

## 11.3 Redis/RabbitMQ problems
- If unavailable, disable via corresponding `Enabled`/feature flags.
- Confirm host/port/credentials and network accessibility.

## 11.4 Frontend API errors (401/403/429/CORS)
- Ensure backend is running on configured `apiUrl`.
- Verify token is present and sent by interceptor.
- Recheck CORS policy for frontend origin.
- Inspect rate-limit headers and retry window.

## 12. Suggested Developer Workflow

1. Start backend first.
2. Start frontend and verify login/product listing/cart flow.
3. Run backend tests before pushing.
4. Build frontend (`npm run build`) before release packaging.
5. Keep all secrets externalized before deployment.
