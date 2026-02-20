# E-Commerce POC

Enterprise-style full-stack e-commerce proof of concept with:
- Backend API in `EcommerceAPI/BulkyBook-POC`
- Frontend UI in `ecommerce-ui`

This repository includes implementation and operations notes for authentication, OAuth, payments, email, caching, messaging, observability, resilience, and rate limiting.

## 1. Features

- JWT authentication and role-based authorization
- OAuth flows (Google + Microsoft)
- Product, category, cart, wishlist, checkout, and order workflows
- Admin operations for users, orders, and reporting
- Stripe payment integration
- SendGrid email integration
- Redis cache abstraction
- RabbitMQ/MassTransit messaging integration
- Structured logging and global exception handling
- Backend and frontend automated tests

## 2. Repository Layout

```text
POC/
  EcommerceAPI/
    BulkyBook-POC/
      Ecommerce.API/              ASP.NET Core Web API host
      Ecommerce.Application/      CQRS handlers, DTOs, use cases
      Ecommerce.Domain/           Entities and domain interfaces
      Ecommerce.Infrastructure/   EF Core, integrations, service wiring
      Ecommerce.Tests/            xUnit test project
      scripts/                    Backend utility scripts
      Guidline/                   Backend implementation and ops docs
      Ecommerce.sln               Backend solution file
  ecommerce-ui/                   Angular frontend app
  scripts/                        Root helper scripts (including Sonar)
  .github/workflows/              GitHub Actions workflows
  README.md                       Primary onboarding + runbook
  ROOT_INDEX.md                   Quick navigation index
```

## 3. Tech Stack

### Backend
- .NET / ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- MediatR (CQRS pattern)
- Serilog + Application Insights
- JWT auth + OAuth providers
- Rate limiting middleware
- Redis cache services
- MassTransit + RabbitMQ
- Stripe and SendGrid integrations
- Polly resilience policies

### Frontend
- Angular (standalone components)
- RxJS
- Bootstrap + SCSS
- Chart.js/ng2-charts
- Toastr notifications

### Testing
- Backend: xUnit, Moq, FluentAssertions, AutoFixture
- Frontend: Karma/Jasmine

## 4. Prerequisites

- Windows + PowerShell
- .NET SDK
- Node.js 20+ and npm
- SQL Server or LocalDB
- Docker Desktop (recommended for full local stack)
- Optional local dependencies:
  - Redis
  - RabbitMQ
  - SonarQube (for local analysis)

## 5. Quick Start (Local)

From repository root:

```powershell
cd D:\POC_NEW\POC
```

### Backend

```powershell
dotnet restore EcommerceAPI\BulkyBook-POC\Ecommerce.sln
dotnet build EcommerceAPI\BulkyBook-POC\Ecommerce.sln
dotnet run --project EcommerceAPI\BulkyBook-POC\Ecommerce.API
```

Default API endpoints (local):
- `https://localhost:7273`
- `https://localhost:7273/swagger`

### Frontend

```powershell
cd ecommerce-ui
npm install
npm start
```

Default UI endpoint:
- `http://localhost:4200`

## 6. Docker Workflow

### Start full stack

```powershell
cd D:\POC_NEW\POC
Copy-Item .env.example .env
docker compose up --build -d
```

Typical endpoints:
- UI: `http://localhost:4200`
- API: `http://localhost:7273`
- Swagger: `http://localhost:7273/swagger`
- RabbitMQ Management: `http://localhost:15672`

### Stop stack

```powershell
docker compose down
```

## 7. SonarQube Analysis

### Local SonarQube with Docker profile

```powershell
docker compose --profile quality up -d sonarqube-db sonarqube
```

Local SonarQube URL:
- `http://localhost:9000`

### Run local analysis scripts

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-sonar-api.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\run-sonar-ui.ps1
```

## 8. CI Pipeline (GitHub Actions)

Workflow file:
- `.github/workflows/build.yml`

Trigger:
- Push to branch `Dev`

What the pipeline does:
- Checks out repository with full history
- Sets up JDK 17
- Caches Sonar scanner artifacts
- Validates required Sonar secrets
- Builds backend solution
- Runs Sonar scanner `begin` and `end`

Required GitHub secrets:
- `SONAR_TOKEN`
- `SONAR_HOST_URL`

Important:
- `SONAR_HOST_URL` must be reachable from GitHub-hosted runners.
- Do not use `localhost` for `SONAR_HOST_URL` in GitHub Actions.
- If SonarQube is only available on local/private network, run CI on a self-hosted runner.

## 9. Build and Test Commands

### Backend build

```powershell
dotnet build EcommerceAPI\BulkyBook-POC\Ecommerce.sln
```

### Backend tests

```powershell
dotnet test EcommerceAPI\BulkyBook-POC\Ecommerce.sln -c Debug
```

### Backend coverage

```powershell
dotnet test EcommerceAPI\BulkyBook-POC\Ecommerce.sln --collect:"XPlat Code Coverage"
```

### Frontend tests

```powershell
cd ecommerce-ui
npm test
```

### Frontend production build

```powershell
cd ecommerce-ui
npm run build
```

Build output:
- `ecommerce-ui/dist/ecommerce-ui`

## 10. Configuration Guide

### Backend configuration files
- `EcommerceAPI/BulkyBook-POC/Ecommerce.API/appsettings.json`
- `EcommerceAPI/BulkyBook-POC/Ecommerce.API/appsettings.Development.json`
- `EcommerceAPI/BulkyBook-POC/Ecommerce.API/appsettings.Production.json`

Common backend configuration sections:
- `ConnectionStrings`
- `Jwt`
- `OAuth` / `GoogleOAuth`
- `Stripe`
- `SendGrid`
- `Redis`
- `RabbitMQ`
- `RateLimiting`
- `KeyVault`
- `Serilog`

### Frontend configuration files
- `ecommerce-ui/src/environments/environment.ts`
- `ecommerce-ui/src/environments/environment.prod.ts`

Common frontend settings:
- `apiUrl`
- OAuth client settings
- Stripe publishable key
- rate-limiting related values

## 11. Secrets and Security

Never commit real secrets to source control.

Use:
- .NET User Secrets for local development
- Environment variables for runtime/pipelines
- Azure Key Vault (or equivalent secret manager) for production

Reference docs:
- `WHERE_TO_STORE_SECRETS.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/SECRETS_MANAGEMENT_GUIDE.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/SECRETS_MIGRATION_CHECKLIST.md`

## 12. Troubleshooting

### `dotnet build` fails with MSB1003
Cause:
- You are running build from a folder without a `.sln`/`.csproj`.

Fix:
```powershell
dotnet build EcommerceAPI\BulkyBook-POC\Ecommerce.sln
```

### Sonar scanner reports invalid `sonar.token` / `sonar.host.url`
Cause:
- Empty/missing GitHub secrets.

Fix:
- Set `SONAR_TOKEN` and `SONAR_HOST_URL` in repository secrets.

### Sonar scanner cannot connect (`localhost:9000` refused)
Cause:
- GitHub-hosted runner cannot access your local SonarQube.

Fix:
- Use publicly reachable SonarQube URL, or
- Use a self-hosted GitHub runner on the same network as SonarQube.

### Frontend receives 401/403/429 or CORS issues
Checklist:
- Backend is running and reachable at configured `apiUrl`
- JWT is being sent by interceptor
- CORS policy allows frontend origin
- Rate-limit windows/headers are understood by client

## 13. Additional Project Documentation

Root-level docs:
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

Backend guideline docs:
- `EcommerceAPI/BulkyBook-POC/Guidline/README.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/HOW_TO_RUN_TESTS.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/RATE_LIMITING_TEST_GUIDE.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/RABBITMQ_IMPLEMENTATION_GUIDE.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/RESILIENCE_TESTING_GUIDE.md`

## 14. Recommended Dev Flow

1. Restore/build backend solution.
2. Run backend API and verify Swagger loads.
3. Start frontend and verify login + product listing.
4. Run backend and frontend tests before pushing.
5. Externalize all secrets before any deployment/CI run.
