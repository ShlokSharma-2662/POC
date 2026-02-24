# E-Commerce POC

Enterprise-style full-stack e-commerce proof of concept demonstrating authentication, payments, messaging, observability, and resilience patterns.

| Component | Technology |
|-----------|------------|
| **Backend** | ASP.NET Core 8 · CQRS (MediatR) · Entity Framework Core · SQL Server |
| **Frontend** | Angular 19 · Bootstrap 5 · RxJS |
| **Infra** | Docker Compose · Redis · RabbitMQ · SonarQube |

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Repository Layout](#repository-layout)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Docker Workflow](#docker-workflow)
- [Build & Test](#build-and-test)
- [Configuration](#configuration)
- [Secrets & Security](#secrets-and-security)
- [CI/CD](#cicd)
- [SonarQube Analysis](#sonarqube-analysis)
- [Troubleshooting](#troubleshooting)
- [Documentation Index](#documentation-index)

---

## Features

| Area | Capabilities |
|------|--------------|
| **Auth** | JWT + role-based authorization, OAuth (Google, Microsoft) |
| **E-Commerce** | Products, categories, cart, wishlist, checkout, orders |
| **Admin** | User management, order management, reporting |
| **Integrations** | Stripe payments, SendGrid email |
| **Infrastructure** | Redis caching, RabbitMQ/MassTransit messaging |
| **Observability** | Structured logging (Serilog), Application Insights |
| **Resilience** | Polly policies, rate limiting (IP + client) |
| **Quality** | Backend (xUnit) and frontend (Karma/Jasmine) automated tests |

---

## Architecture

```
┌─────────────────┐     ┌──────────────────────────────────────────────────┐
│   Angular 19    │     │              ASP.NET Core 8 API                   │
│   (Port 4200)   │────▶│  CQRS · JWT · OAuth · Rate Limiting · Polly       │
└─────────────────┘     └──────────────────────────────────────────────────┘
                                        │
        ┌───────────────────────────────┼───────────────────────────────┐
        ▼               ▼               ▼               ▼               ▼
   ┌─────────┐   ┌──────────┐   ┌───────────┐   ┌──────────┐   ┌────────────┐
   │SQL      │   │  Redis   │   │ RabbitMQ  │   │ Stripe   │   │ SendGrid   │
   │Server   │   │  Cache   │   │ Messaging │   │ Payments │   │ Email      │
   └─────────┘   └──────────┘   └───────────┘   └──────────┘   └────────────┘
```

---

## Repository Layout

```
POC/
├── EcommerceAPI/
│   └── BulkyBook-POC/
│       ├── Ecommerce.API/           # Web API host
│       ├── Ecommerce.Application/   # CQRS handlers, DTOs, use cases
│       ├── Ecommerce.Domain/        # Entities and domain interfaces
│       ├── Ecommerce.Infrastructure/# EF Core, integrations, DI
│       ├── Ecommerce.Tests/         # xUnit tests
│       ├── Guidline/                # Backend guides and docs
│       ├── scripts/                 # Backend utility scripts
│       └── Ecommerce.sln
├── ecommerce-ui/                    # Angular frontend
├── scripts/                         # Root scripts (Sonar, etc.)
├── .github/workflows/               # GitHub Actions
├── docker-compose.yml               # Full stack + optional SonarQube
├── .env.example                     # Environment template
└── README.md
```

---

## Tech Stack

### Backend

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- MediatR (CQRS)
- Serilog + Application Insights
- JWT + OAuth (Google, Microsoft)
- Rate limiting middleware (IP + client)
- Redis cache abstraction
- MassTransit + RabbitMQ
- Stripe, SendGrid
- Polly resilience policies

### Frontend

- Angular 19 (standalone components)
- RxJS, Bootstrap 5, SCSS
- Chart.js / ng2-charts
- ngx-toastr, Stripe.js

### Testing

- Backend: xUnit, Moq, FluentAssertions, AutoFixture
- Frontend: Karma, Jasmine

---

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| **Windows + PowerShell** | Primary dev environment |
| **.NET SDK 8** | `dotnet --version` |
| **Node.js 20+** | For Angular |
| **SQL Server / LocalDB** | Local development |
| **Docker Desktop** | For full stack and optional services |
| **Optional** | Redis, RabbitMQ (or use Docker) |

---

## Quick Start

### 1. Clone and navigate

```powershell
cd D:\POC_NEW\POC
```

### 2. Backend

```powershell
dotnet restore EcommerceAPI\BulkyBook-POC\Ecommerce.sln
dotnet build EcommerceAPI\BulkyBook-POC\Ecommerce.sln
dotnet run --project EcommerceAPI\BulkyBook-POC\Ecommerce.API
```

- API: `https://localhost:7273`
- Swagger: `https://localhost:7273/swagger`

### 3. Frontend

```powershell
cd ecommerce-ui
npm install
npm start
```

- UI: `http://localhost:4200`

---

## Docker Workflow

### First-time setup

```powershell
Copy-Item .env.example .env
# Edit .env and set SQL_SA_PASSWORD, JWT_SECRET_KEY
```

### Start full stack

```powershell
docker compose up --build -d
```

| Service | URL |
|---------|-----|
| UI | http://localhost:4200 |
| API | http://localhost:7273 |
| Swagger | http://localhost:7273/swagger |
| RabbitMQ Management | http://localhost:15672 |

### Stop

```powershell
docker compose down
```

### Environment variables (`.env`)

| Variable | Description | Example |
|----------|-------------|---------|
| `SQL_SA_PASSWORD` | SQL Server SA password | `YourStrong@Passw0rd` |
| `JWT_SECRET_KEY` | JWT signing key | Change for production |
| `SONARQUBE_DB_*` | SonarQube (quality profile) | See `.env.example` |
| `SONAR_HOST_URL` | SonarQube URL | `http://localhost:9000` |
| `SONAR_TOKEN` | SonarQube auth token | From SonarQube UI |

---

## Build and Test

### Backend

```powershell
# Build
dotnet build EcommerceAPI\BulkyBook-POC\Ecommerce.sln

# Run tests
dotnet test EcommerceAPI\BulkyBook-POC\Ecommerce.sln -c Debug

# Code coverage
dotnet test EcommerceAPI\BulkyBook-POC\Ecommerce.sln --collect:"XPlat Code Coverage"
```

### Frontend

```powershell
cd ecommerce-ui

# Run tests
npm test

# Production build
npm run build
# Output: ecommerce-ui/dist/ecommerce-ui/
```

---

## Configuration

### Backend

- `EcommerceAPI/BulkyBook-POC/Ecommerce.API/appsettings.json`
- `appsettings.Development.json`, `appsettings.Production.json`

Key sections: `ConnectionStrings`, `Jwt`, `OAuth`, `GoogleOAuth`, `Stripe`, `SendGrid`, `Redis`, `RabbitMQ`, `RateLimiting`, `KeyVault`, `Serilog`, `ApplicationInsights`.

### Frontend

- `ecommerce-ui/src/environments/environment.ts`
- `ecommerce-ui/src/environments/environment.prod.ts`

Key settings: `apiUrl`, OAuth client IDs, Stripe publishable key.

---

## Secrets and Security

**Never commit real secrets to source control.**

| Environment | Approach |
|-------------|----------|
| Local dev | .NET User Secrets |
| Runtime / CI | Environment variables |
| Production | Azure Key Vault or equivalent |

Reference docs:

- `WHERE_TO_STORE_SECRETS.md` (if present)
- `EcommerceAPI/BulkyBook-POC/Guidline/SECRETS_MANAGEMENT_GUIDE.md`
- `EcommerceAPI/BulkyBook-POC/Guidline/SECRETS_MIGRATION_CHECKLIST.md`

---

## CI/CD

### Workflow

- **File:** `.github/workflows/dotnet.yml`
- **Triggers:** Push and pull requests to `Dev`
- **Actions:** Restore, build (.NET 8)

```yaml
on:
  push:
    branches: [ "Dev" ]
  pull_request:
    branches: [ "Dev" ]
```

### Future enhancements

For SonarQube in CI: configure `SONAR_TOKEN` and `SONAR_HOST_URL` (must be reachable from GitHub runners; use a cloud SonarQube or self-hosted runner).

---

## SonarQube Analysis

### Start SonarQube (Docker)

```powershell
docker compose --profile quality up -d sonarqube-db sonarqube
```

- SonarQube: http://localhost:9000

### Run local analysis

```powershell
# From repo root; requires .env with SONAR_TOKEN and SONAR_HOST_URL
.\scripts\run-sonar-api.ps1
.\scripts\run-sonar-ui.ps1
```

---

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| `MSB1003` on `dotnet build` | Wrong directory | Use full path: `dotnet build EcommerceAPI\BulkyBook-POC\Ecommerce.sln` |
| Invalid Sonar token/URL | Missing env vars | Set `SONAR_TOKEN`, `SONAR_HOST_URL` in `.env` or environment |
| Sonar `localhost:9000` refused (CI) | GitHub runner can't reach local SonarQube | Use cloud SonarQube or self-hosted runner |
| Frontend 401/403/429 or CORS | API unreachable, auth, or rate limits | Check `apiUrl`, JWT interceptor, CORS, rate-limit headers |

---

## Documentation Index

### Root-level docs

- `E-Commerce_Project_Presentation.md`
- `API_RESPONSE_REFACTORING_SUMMARY.md`, `UI_RESPONSE_REFACTORING_SUMMARY.md`
- `GOOGLE_OAUTH_IMPLEMENTATION.md`, `GUEST_USER_IMPLEMENTATION.md`
- `REDIS_COMPREHENSIVE_IMPLEMENTATION.md`, `REDIS_IMPLEMENTATION_SUMMARY.md`
- `EMAIL_IMPLEMENTATION_SUMMARY.md`, `EMAIL_DIAGNOSTIC_REPORT.md`, `SENDGRID_SETUP.md`

### Backend guidelines

- `EcommerceAPI/BulkyBook-POC/Guidline/README.md`
- `HOW_TO_RUN_TESTS.md`, `RATE_LIMITING_TEST_GUIDE.md`
- `RABBITMQ_IMPLEMENTATION_GUIDE.md`, `RESILIENCE_TESTING_GUIDE.md`

---

## Recommended Dev Flow

1. Restore and build the backend; verify Swagger at `https://localhost:7273/swagger`.
2. Start the frontend; verify login and product listing.
3. Run backend and frontend tests before pushing.
4. Keep all secrets out of source; use User Secrets or env vars.
