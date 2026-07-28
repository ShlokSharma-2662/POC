# E-Commerce POC — Scalable Enterprise Management System

Enterprise-grade full-stack e-commerce system built as a **modular, cloud-native, event-driven, API-first** application. Demonstrates microservices architecture, CQRS, real-time GraphQL subscriptions, gRPC inter-service communication, Azure Functions for task automation, and comprehensive observability.

| Component | Technology |
|-----------|------------|
| **Backend** | ASP.NET Core 9 · CQRS (MediatR) · Entity Framework Core · SQL Server |
| **Microservices** | Ecommerce.API · OrderService · ProductService · Azure Functions |
| **API Layers** | REST · GraphQL (Hot Chocolate) · gRPC · OData |
| **Frontend** | React 19 · Vite · TypeScript · Bootstrap 5 (Angular 19 retained during migration) |
| **Infra** | Docker Compose · Redis · RabbitMQ · Azure Event Grid · SonarQube |

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Repository Layout](#repository-layout)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Local Database Setup](#local-database-setup)
- [Docker Workflow](#docker-workflow)
- [Build & Test](#build-and-test)
- [Configuration](#configuration)
- [Cart Cache Consistency](#cart-cache-consistency)
- [Backend Change Roadmap](#backend-change-roadmap)
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
| **Quality** | Backend (xUnit) and frontend (Vitest/Testing Library/Playwright) automated tests |

---

## Architecture

```
┌─────────────────┐
│    React 19     │
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
├── ecommerce-react/                  # React frontend
├── ecommerce-ui/                     # Legacy Angular frontend / rollback artifact
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

- React 19, TypeScript, Vite, React Router
- TanStack Query, Zustand, Bootstrap 5, SCSS
- Chart.js / react-chartjs-2
- Sonner, React Stripe.js
- Angular 19 remains available in `ecommerce-ui/` as the rollback artifact

### Testing

- Backend: xUnit, Moq, FluentAssertions
- Frontend: Vitest, Testing Library, Playwright

---

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| **Windows + PowerShell** | Primary dev environment |
| **.NET SDK 9** | `dotnet --version` |
| **Node.js 22+** | For the React/Vite frontend |
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
dotnet run --project EcommerceAPI\BulkyBook-POC\Ecommerce.OrderService
```

- API: `https://localhost:7273`
- Swagger: `https://localhost:7273/swagger`
- GraphQL Playground: `https://localhost:7273/graphql`

Seed the local `ECommercePOC` LocalDB with repeatable development data:

```powershell
.\scripts\seed-localdb.ps1
```

- Admin login: `admin@local.test` / `Admin123!`
- User login: `user@local.test` / `User123!`
- The seed is local-development-only and can be run repeatedly without
  duplicating its users, categories, or products.

### 3. Frontend

```powershell
cd ecommerce-react
npm ci
npm run dev
```

- UI: `http://localhost:4200`
- Migration and cutover details: `docs/REACT_MIGRATION.md`

---

## Local Database Setup

The local backend uses SQL Server LocalDB by default.

| Connection field | Value |
|------------------|-------|
| Connection type | Microsoft SQL Server |
| Server | `(localdb)\MSSQLLocalDB` |
| Authentication | Windows Authentication |
| Database | `ECommercePOC` |

The matching development connection string is:

```text
Server=(localdb)\MSSQLLocalDB;Database=ECommercePOC;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

Create or refresh the development data from the repository root:

```powershell
.\scripts\seed-localdb.ps1
```

The script starts the `MSSQLLocalDB` instance, creates the database when
required, applies the development data, and can be run repeatedly.

If you do not have LocalDB installed, use the Docker workflow instead. The
Docker database is named `EcommerceDB` and is exposed on `localhost,1433`.

---

## Docker Workflow

### First-time setup

```powershell
Copy-Item .env.example .env
# Edit .env and set SQL_SA_PASSWORD, JWT_SECRET_KEY, and both Stripe keys
```

### Start full stack

```powershell
docker compose up --build -d
```

Before enabling checkout against an existing database, apply
`EcommerceAPI\BulkyBook-POC\Ecommerce.Infrastructure\Persistence\Migrations\20260728_secure_checkout.sql`.
See `docs\REACT_MIGRATION.md` for OAuth callbacks, required secrets, and the
credentialed release gates.

Seed the Docker database after the stack has been created:

```powershell
.\scripts\seed-docker.ps1
```

The runner seeds `EcommerceDB` inside `ecommerce-sqlserver`, reads the SA
password from the container environment, and invalidates stale product,
category, and user cache entries. It does not print or copy the database
password to the host command line.

| Service | URL |
|---------|-----|
| UI | http://localhost:4200 |
| API | http://localhost:7273 |
| OrderService | http://localhost:7274 |
| Swagger | http://localhost:7273/swagger |
| GraphQL | http://localhost:7273/graphql |
| RabbitMQ Management | http://localhost:15672 |

### Stop

```powershell
docker compose down
```

The retained Angular rollback can be started separately at
`http://localhost:4201` with
`docker compose --profile legacy up -d ecommerce-angular`.

### Environment variables (`.env`)

| Variable | Description | Example |
|----------|-------------|---------|
| `SQL_SA_PASSWORD` | SQL Server SA password | `YourStrong@Passw0rd` |
| `JWT_SECRET_KEY` | JWT signing key | Change for production |
| `STRIPE_PUBLISHABLE_KEY` | Public Stripe key compiled into the React UI | `pk_test_...` |
| `STRIPE_SECRET_KEY` | Server-only Stripe API key; never expose it to Vite | `sk_test_...` |
| `FRONTEND_BASE_URL` | Canonical browser origin used for redirects and CORS | `http://localhost:4200` |
| `PUBLIC_APP_BASE_URL` | Public origin through which OAuth reaches `/api` callbacks | `http://localhost:4200` |
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
cd ecommerce-react

# Typecheck, lint, and tests
npm run typecheck
npm run lint
npm test

# Production build
npm run build
# Output: ecommerce-react/dist/

# Desktop and mobile browser smoke tests
npx playwright install chromium
npm run e2e
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

- `ecommerce-react/.env.example`
- `ecommerce-react/src/config/environment.ts`

Key public settings: `VITE_API_BASE_URL`, `VITE_DEV_API_TARGET`,
`VITE_DEV_ORDER_API_TARGET`, and `VITE_STRIPE_PUBLISHABLE_KEY`.

---

## Cart Cache Consistency

Authenticated carts use SQL Server as the source of truth and Redis as a
read-through cache. Each cached cart uses the key `user_{userId}_cart`.

The backend invalidates that user-specific key after every successful cart
mutation:

- add an item;
- change an item quantity;
- remove an item;
- clear the cart.

This prevents navigation to `/cart` from loading an older Redis value and
overwriting the current React cart state. No React-side workaround is needed.

Existing cache entries created before this fix can be inspected and removed
once:

```powershell
docker compose exec -T redis redis-cli --scan --pattern "user_*_cart"

# Delete only keys returned by the scan, for example:
docker compose exec -T redis redis-cli UNLINK user_1_cart user_2_cart
```

Do not use `FLUSHDB`; Redis also contains other application data. Deleted cart
keys are recreated automatically from SQL Server on the next cart request.

---

## Backend Change Roadmap

The React migration does not require a backend rewrite. Continue with the
existing ASP.NET Core services and make changes incrementally.

| Priority | Change | Status |
|----------|--------|--------|
| P0 | Invalidate Redis cart data after add, update, delete, and clear | Completed |
| P0 | Keep secrets outside source control and validate required settings at startup | Completed |
| P1 | Standardize REST response DTOs, validation errors, and HTTP status codes | Next |
| P1 | Add integration tests covering React -> API -> Redis -> SQL workflows | Next |
| P1 | Revalidate stock and use concurrency protection during checkout | Next |
| P1 | Add idempotency protection for payment and checkout requests | Next |
| P2 | Add SQL Server, Redis, and RabbitMQ health checks | Planned |
| P2 | Add request, user, and message correlation IDs to structured logs | Planned |
| P2 | Review access-token storage and introduce a refresh-token strategy | Planned |

Recommended implementation order:

1. Standardize API errors and DTO contracts.
2. Add authenticated cart and checkout integration tests.
3. Add checkout concurrency and idempotency controls.
4. Add dependency health checks and correlation logging.
5. Harden the long-lived authentication flow.

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
- **Actions:** Build/test .NET 9; typecheck, lint, test, build, browser-test, and containerize React

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
| Application requests `Microsoft.NETCore.App 9.0.0` | Only another major .NET runtime is installed | Install the x64 .NET 9 SDK/runtime side by side, then confirm with `dotnet --list-runtimes` |
| Cannot connect to LocalDB | Wrong instance or authentication | Use `(localdb)\MSSQLLocalDB`, Windows Authentication, and database `ECommercePOC` |
| Cart becomes empty after navigation | A Redis cart entry predates the cache-invalidation fix | Rebuild/restart `ecommerce-api`, scan `user_*_cart`, and delete only the stale cart keys |
| Invalid Sonar token/URL | Missing env vars | Set `SONAR_TOKEN`, `SONAR_HOST_URL` in `.env` or environment |
| Sonar `localhost:9000` refused (CI) | GitHub runner can't reach local SonarQube | Use cloud SonarQube or self-hosted runner |
| Frontend 401/403/429 or CORS | API unreachable, auth, or rate limits | Check `VITE_API_BASE_URL`, JWT storage, CORS, and rate-limit headers |

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
| **Ecommerce.OrderService** | 7274 | Order CRUD, status updates, GraphQL subscriptions |
| **Ecommerce.ProductService** | — | Product catalog, GraphQL queries/mutations, OData |
| **Ecommerce.Functions** | — | Azure Durable Functions: order fulfillment orchestrator, EventGrid triggers, dead letter processing, stock replenishment |
