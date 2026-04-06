# E-Commerce POC — Scalable Enterprise Management System

Enterprise-grade full-stack e-commerce system built as a **modular, cloud-native, event-driven, API-first** application. Demonstrates microservices architecture, CQRS, real-time GraphQL subscriptions, gRPC inter-service communication, Azure Functions for task automation, and comprehensive observability.

| Component | Technology |
|-----------|------------|
| **Backend** | ASP.NET Core 9 · CQRS (MediatR) · Entity Framework Core · SQL Server |
| **Microservices** | Ecommerce.API · OrderService · ProductService · Azure Functions |
| **API Layers** | REST · GraphQL (Hot Chocolate) · gRPC · OData |
| **Frontend** | Angular 19 · Bootstrap 5 · RxJS |
| **Infra** | Docker Compose · Redis · RabbitMQ · Azure Event Grid · SonarQube |

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
| **Admin** | User management, order management, revenue reporting, performance dashboard |
| **Microservices** | Dedicated OrderService, ProductService, and Azure Functions project |
| **API Stack** | REST, GraphQL (queries, mutations, subscriptions), gRPC, OData |
| **Event-Driven** | Azure Event Grid, RabbitMQ/MassTransit, GraphQL subscriptions (WebSocket) |
| **Cloud Automation** | Azure Durable Functions (order fulfillment orchestrator), EventGrid triggers, dead letter processing |
| **Integrations** | Stripe payments, SendGrid email |
| **Caching** | Redis distributed cache with invalidation, compiled EF Core queries |
| **Observability** | Serilog, Application Insights, OpenTelemetry tracing |
| **Validation** | FluentValidation pipeline behaviors (validation, logging, performance) |
| **Resilience** | Polly policies, rate limiting (IP + client) |
| **Quality** | Backend (xUnit) and frontend (Karma/Jasmine) automated tests |

---

## Architecture

```
┌─────────────────┐
│   Angular 19    │
│   (Port 4200)   │
└────────┬────────┘
         │ HTTP / WebSocket
         ▼
┌──────────────────────────────────────────────────────────┐
│                    Ecommerce.API                         │
│  REST + GraphQL + gRPC + OData                           │
│  JWT Auth │ Rate Limiting │ Serilog │ OpenTelemetry       │
└────────┬──────────────┬──────────────┬───────────────────┘
         │              │              │
    ┌────▼────┐   ┌─────▼─────┐  ┌────▼──────────┐
    │ Order   │   │ Product   │  │ Azure         │
    │ Service │   │ Service   │  │ Functions     │
    │ GraphQL │   │ GraphQL   │  │ Durable       │
    │ CQRS    │   │ OData     │  │ EventGrid     │
    └────┬────┘   └─────┬─────┘  └────┬──────────┘
         │              │              │
    ┌────▼──────────────▼──────────────▼───────────┐
    │           Shared Infrastructure              │
    │  EF Core │ Redis │ MassTransit │ Event Grid   │
    │  Serilog │ App Insights │ FluentValidation    │
    └────────────────────┬─────────────────────────┘
                         │
     ┌───────────┬───────┼────────┬─────────────┐
     ▼           ▼       ▼        ▼             ▼
┌─────────┐ ┌────────┐ ┌───────┐ ┌──────────┐ ┌────────┐
│SQL      │ │ Redis  │ │Rabbit │ │ Stripe   │ │SendGrid│
│Server   │ │ Cache  │ │MQ     │ │ Payments │ │ Email  │
└─────────┘ └────────┘ └───────┘ └──────────┘ └────────┘
```

---

## Repository Layout

```
POC/
├── EcommerceAPI/
│   └── BulkyBook-POC/
│       ├── Ecommerce.API/            # Main API host (REST + GraphQL + gRPC + OData)
│       ├── Ecommerce.OrderService/   # Order microservice (GraphQL subscriptions)
│       ├── Ecommerce.ProductService/ # Product microservice (GraphQL + OData)
│       ├── Ecommerce.Functions/      # Azure Functions (Durable, EventGrid, timers)
│       ├── Ecommerce.Application/    # CQRS handlers, validators, DTOs, behaviors
│       ├── Ecommerce.Domain/         # Entities, domain interfaces, event sourcing
│       ├── Ecommerce.Infrastructure/ # EF Core, Redis, messaging, DI
│       ├── Ecommerce.Tests/          # xUnit tests
│       ├── Guideline/                # Backend guides and docs
│       ├── scripts/                  # Backend utility scripts
│       └── Ecommerce.sln
├── ecommerce-ui/                     # Angular frontend
├── assets/                           # Product images
├── config/                           # Web.config variants
├── docs/                             # All project documentation
├── scripts/                          # Root scripts (Sonar, scaffolding)
├── .github/workflows/                # GitHub Actions
├── docker-compose.yml                # Full stack + optional SonarQube
├── .env.example                      # Environment template
└── README.md
```

---

## Tech Stack

### Backend

- .NET 9 / ASP.NET Core Web API
- Entity Framework Core (SQL Server) with compiled queries
- MediatR (CQRS) with pipeline behaviors
- FluentValidation (command validators)
- Hot Chocolate (GraphQL queries, mutations, subscriptions)
- gRPC (Protobuf service definitions)
- OData (advanced filtering & pagination)
- OpenTelemetry (ASP.NET Core, HttpClient, SqlClient tracing)
- Serilog + Application Insights
- JWT + OAuth (Google, Microsoft)
- Rate limiting middleware (IP + client)
- Redis distributed cache with pattern-based invalidation
- MassTransit + RabbitMQ
- Azure Event Grid (cross-service event publishing)
- Azure Durable Functions (order fulfillment orchestrator)
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
| **.NET SDK 9** | `dotnet --version` |
| **Node.js 20+** | For Angular |
| **SQL Server / LocalDB** | Local development |
| **Docker Desktop** | For full stack and optional services |
| **Optional** | Redis, RabbitMQ, Azure Functions Core Tools (or use Docker) |

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
- GraphQL Playground: `https://localhost:7273/graphql`

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
| GraphQL | http://localhost:7273/graphql |
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

Key sections: `ConnectionStrings`, `Jwt`, `OAuth`, `GoogleOAuth`, `Stripe`, `SendGrid`, `Redis`, `RabbitMQ`, `RateLimiting`, `KeyVault`, `Serilog`, `ApplicationInsights`, `OpenTelemetry`.

Each microservice has its own appsettings:
- `Ecommerce.OrderService/appsettings.json`
- `Ecommerce.ProductService/appsettings.json`
- `Ecommerce.Functions/local.settings.json`

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

- `docs/WHERE_TO_STORE_SECRETS.md`
- `EcommerceAPI/BulkyBook-POC/Guideline/SECRETS_MANAGEMENT_GUIDE.md`
- `EcommerceAPI/BulkyBook-POC/Guideline/SECRETS_MIGRATION_CHECKLIST.md`

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

### Project docs (`docs/`)

- `E-Commerce_Project_Presentation.md` — Full project presentation
- `CODEBASE_ANALYSIS.md` — Architecture and codebase overview
- `API_RESPONSE_REFACTORING_SUMMARY.md`, `UI_RESPONSE_REFACTORING_SUMMARY.md`
- `GOOGLE_OAUTH_IMPLEMENTATION.md`, `GUEST_USER_IMPLEMENTATION.md`
- `REDIS_COMPREHENSIVE_IMPLEMENTATION.md`, `REDIS_IMPLEMENTATION_SUMMARY.md`
- `EMAIL_IMPLEMENTATION_SUMMARY.md`, `EMAIL_DIAGNOSTIC_REPORT.md`, `SENDGRID_SETUP.md`
- `WHERE_TO_STORE_SECRETS.md`, `Z_INDEX_SYSTEM.md`
- `ECommerce Project Estimation.xlsx`

### Backend guidelines (`EcommerceAPI/BulkyBook-POC/Guideline/`)

- `README.md` — Getting started
- `HOW_TO_RUN_TESTS.md`, `TEST_COVERAGE_SUMMARY.md`
- `RATE_LIMITING_TEST_GUIDE.md`, `RATE_LIMITING_ENHANCEMENTS_COMPLETE.md`
- `RABBITMQ_IMPLEMENTATION_GUIDE.md`
- `RESILIENCE_TESTING_GUIDE.md`, `RESILIENCE_ENHANCEMENTS_COMPLETE.md`
- `SECRETS_MANAGEMENT_GUIDE.md`, `SECRETS_MIGRATION_CHECKLIST.md`
- `AUTHENTICATION_AUTHORIZATION_COMPLETE.md`

---

## Recommended Dev Flow

1. Restore and build the backend; verify Swagger at `https://localhost:7273/swagger`.
2. Verify GraphQL playground at `https://localhost:7273/graphql`.
3. Start the frontend; verify login and product listing.
4. Run backend and frontend tests before pushing.
5. Keep all secrets out of source; use User Secrets or env vars.

## Microservices

| Service | Port | Responsibilities |
|---------|------|------------------|
| **Ecommerce.API** | 7273 | Main gateway — REST, GraphQL, gRPC, OData, admin endpoints |
| **Ecommerce.OrderService** | — | Order CRUD, status updates, GraphQL subscriptions |
| **Ecommerce.ProductService** | — | Product catalog, GraphQL queries/mutations, OData |
| **Ecommerce.Functions** | — | Azure Durable Functions: order fulfillment orchestrator, EventGrid triggers, dead letter processing, stock replenishment |
