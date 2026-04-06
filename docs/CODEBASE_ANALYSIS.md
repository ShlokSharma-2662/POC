# E-Commerce POC - Codebase Analysis

**Analysis Date:** 2026-03-23

---

## 1. Project Overview

### Project Purpose
Enterprise-style full-stack e-commerce proof of concept demonstrating authentication, payments, messaging, observability, and resilience patterns.

### Architecture Overview
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

## 2. Technology Stack

### Backend
| Component | Technology | Version |
|-----------|------------|---------|
| Runtime | .NET | 10.0.201 (SDK) |
| Framework | ASP.NET Core | 8 |
| ORM | Entity Framework Core | Latest |
| Architecture | CQRS with MediatR | Latest |
| Database | SQL Server / LocalDB | 2022 |
| Authentication | JWT Bearer + OAuth (Microsoft, Google) | - |
| Caching | Redis + StackExchange.Redis | - |
| Messaging | MassTransit + RabbitMQ | - |
| Logging | Serilog + Application Insights | - |
| Resilience | Polly | - |
| Rate Limiting | AspNetCoreRateLimit | - |
| Validation | FluentValidation | - |
| Testing | xUnit, Moq, FluentAssertions, AutoFixture | - |

### Frontend
| Component | Technology | Version |
|-----------|------------|---------|
| Framework | Angular | 19.2.x |
| Language | TypeScript | 5.7.2 |
| UI Library | Bootstrap | 5.3.3 |
| Styling | SCSS | - |
| HTTP Client | RxJS | 7.8.x |
| Charts | Chart.js + ng2-charts | 4.x / 4.x |
| Notifications | ngx-toastr | 19.x |
| Payments | Stripe.js | 3.x |
| Testing | Karma + Jasmine | 6.x / 5.x |

### Infrastructure
| Service | Purpose |
|---------|---------|
| Docker Compose | Full-stack containerization |
| SQL Server | Primary database |
| Redis | Distributed caching |
| RabbitMQ | Message queue |
| SonarQube | Code quality (optional profile) |
| GitHub Actions | CI/CD pipeline |

---

## 3. Directory Structure

```
D:\POC_NEW\POC\
├── EcommerceAPI/                      # .NET Backend
│   └── BulkyBook-POC/
│       ├── Ecommerce.API/             # Web API host
│       ├── Ecommerce.Application/     # CQRS handlers, DTOs, use cases
│       ├── Ecommerce.Domain/           # Entities and domain interfaces
│       ├── Ecommerce.Infrastructure/  # EF Core, integrations, DI
│       ├── Ecommerce.Tests/           # xUnit tests
│       ├── Ecommerce.ProductService/   # Product microservice
│       ├── Ecommerce.OrderService/     # Order microservice
│       ├── Ecommerce.Functions/       # Azure Functions
│       ├── Guidline/                  # Backend documentation
│       └── Ecommerce.sln              # Solution file
├── ecommerce-ui/                      # Angular Frontend
│   ├── src/
│   │   ├── app/                       # Application code
│   │   ├── environments/              # Environment configs
│   │   └── assets/                    # Static assets
│   └── package.json
├── .github/workflows/                  # GitHub Actions CI/CD
├── docker-compose.yml                 # Docker configuration
├── .env.example                       # Environment template
└── README.md                          # Project documentation
```

---

## 4. Key Source Directories and Their Purposes

### Backend (EcommerceAPI/BulkyBook-POC)

| Directory | Purpose | Key Files |
|-----------|---------|-----------|
| `Ecommerce.API/` | Web API host, entry point | `Program.cs`, `Controllers/`, `Middleware/` |
| `Ecommerce.Application/` | CQRS logic, use cases | `Features/`, `Common/`, `DTOs/` |
| `Ecommerce.Domain/` | Domain entities | `Entities/*.cs` |
| `Ecommerce.Infrastructure/` | External services, DB context | `Services/`, `Persistence/`, `Messaging/` |
| `Ecommerce.Tests/` | Unit and integration tests | `Controllers/`, `Application/` |
| `Ecommerce.ProductService/` | Product microservice | `Controllers/`, `GraphQL/` |
| `Ecommerce.OrderService/` | Order microservice | `Controllers/`, `Commands/`, `Queries/` |
| `Ecommerce.Functions/` | Azure Functions | - |

### Frontend (ecommerce-ui)

| Directory | Purpose | Key Files |
|-----------|---------|-----------|
| `src/app/` | Application code | Components, services, models, guards |
| `src/app/services/` | API services | `product.service.ts`, `order.service.ts`, etc. |
| `src/app/components/` | Reusable UI components | - |
| `src/app/models/` | TypeScript interfaces | `product.ts`, `order.model.ts`, etc. |
| `src/app/guards/` | Route guards | `auth.guard.ts`, `admin.guard.ts` |
| `src/app/interceptors/` | HTTP interceptors | `auth.interceptor.ts`, `rate-limiting.interceptor.ts` |
| `src/environments/` | Environment configs | `environment.ts`, `environment.prod.ts` |

---

## 5. Main Configuration Files

### Backend

| File | Purpose |
|------|---------|
| `Ecommerce.sln` | Visual Studio solution file |
| `global.json` | .NET SDK version (10.0.201) |
| `Ecommerce.API/appsettings.json` | Main configuration (JWT, OAuth, Stripe, Redis, RabbitMQ, Rate Limiting) |
| `Ecommerce.API/appsettings.Development.json` | Dev overrides |
| `Ecommerce.API/appsettings.Production.json` | Production overrides |
| `Directory.Build.props` | MSBuild properties |
| `Directory.Packages.props` | Central package versions |

### Frontend

| File | Purpose |
|------|---------|
| `package.json` | NPM dependencies and scripts |
| `angular.json` | Angular CLI configuration |
| `tsconfig.json` | TypeScript compiler options |
| `tsconfig.app.json` | App-specific TypeScript config |
| `tsconfig.spec.json` | Test TypeScript config |
| `src/environments/environment.ts` | Dev environment config |
| `src/environments/environment.prod.ts` | Production environment config |

---

## 6. Entry Points and Main Modules

### Backend Entry Point
- **File:** `EcommerceAPI/BulkyBook-POC/Ecommerce.API/Program.cs`
- **URL:** `https://localhost:7273`
- **Swagger:** `https://localhost:7273/swagger`

### Frontend Entry Point
- **File:** `ecommerce-ui/src/main.ts`
- **Bootstrap:** `bootstrapApplication(AppComponent, appConfig)`
- **URL:** `http://localhost:4200`

### Key Backend Controllers

| Controller | Route | Purpose |
|------------|-------|---------|
| `AuthController` | `/api/auth` | Login, register, refresh tokens |
| `ProductsController` | `/api/products` | Product CRUD, images |
| `CartController` | `/api/cart` | Cart operations |
| `OrdersController` | `/api/orders` | Order placement, history |
| `WishlistController` | `/api/wishlist` | Wishlist operations |
| `CategoriesController` | `/api/categories` | Category listing |
| `PaymentsController` | `/api/payments` | Stripe integration |
| `AdminController` | `/api/admin` | Admin operations |
| `OAuthController` | `/api/oauth` | Microsoft OAuth |
| `GoogleOAuthController` | `/api/google-oauth` | Google OAuth |

### Key Frontend Components

| Component | Route | Purpose |
|-----------|-------|---------|
| `AppComponent` | `/` | Root component |
| `HomeComponent` | `/home` | Homepage |
| `ProductsComponent` | `/products` | Product listing |
| `ProductDetailComponent` | `/products/:id` | Product details |
| `CartComponent` | `/cart` | Shopping cart |
| `CheckoutComponent` | `/checkout` | Checkout flow |
| `LoginComponent` | `/login` | User login |
| `RegisterComponent` | `/register` | User registration |
| `ProfileComponent` | `/profile` | User profile |
| `MyOrdersComponent` | `/my-orders` | Order history |
| `AdminDashboardComponent` | `/admin` | Admin dashboard |
| `WishlistComponent` | `/wishlist` | Wishlist |

---

## 7. Key Architectural Patterns

### CQRS (Command Query Responsibility Segregation)
The backend follows CQRS pattern using MediatR:

```
Ecommerce.Application/Features/
├── Auth/
│   ├── Commands/     # Write operations
│   └── Queries/      # Read operations
├── Cart/
├── Products/
├── Orders/
├── Wishlist/
├── Admin/
└── Categories/
```

**Pattern:** Each feature has:
- `Command` / `Query` classes (requests)
- `Handler` classes (processing logic)
- `Validator` classes (FluentValidation)

### Standardized API Response
All API responses follow a consistent format via `ApiResponse<T>`:

```csharp
// Ecommerce.Application/Common/Models/ApiResponse.cs
public class ApiResponse<T>
{
    public bool IsSuccessful { get; set; }
    public string Status { get; set; }
    public string StatusReason { get; set; }
    public T? Data { get; set; }
}
```

### Service Layer Pattern (Backend)
- **Controllers** delegate to **MediatR handlers**
- **Handlers** use **Repository interfaces** from Domain
- **Infrastructure** implements repositories via EF Core

### Angular Service Pattern (Frontend)
- **BaseApiService** provides standardized error handling and response parsing
- **Feature services** extend BaseApiService
- **Interceptors** handle auth tokens and rate limiting

### Domain Entities
```csharp
// Ecommerce.Domain/Entities/
├── ApplicationUser.cs
├── Product.cs
├── Category.cs
├── Cart.cs
├── CartItem.cs
├── Order.cs
├── OrderItem.cs
├── WishlistItem.cs
├── RefreshToken.cs
├── SystemMetric.cs
├── ErrorLog.cs
└── ApiAlert.cs
```

### Middleware Pipeline (Backend)
```
Request → SecurityHeadersMiddleware → Session → CORS → Auth → RateLimiting 
→ ErrorLogging → GlobalExceptionHandler → RequestTiming → RequestTimeout 
→ Controller
```

---

## 8. Dependencies and Their Roles

### Backend NuGet Packages

| Package | Role |
|---------|------|
| `MediatR` | CQRS mediator |
| `Microsoft.EntityFrameworkCore` | ORM |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth |
| `Serilog.AspNetCore` | Structured logging |
| `Serilog.Sinks.ApplicationInsights` | App Insights integration |
| `MassTransit.RabbitMQ` | Message queue |
| `StackExchange.Redis` | Redis client |
| `Polly` | Resilience policies |
| `FluentValidation.AspNetCore` | Input validation |
| `AspNetCoreRateLimit` | Rate limiting |
| `BCrypt.Net-Next` | Password hashing |
| `Stripe.net` | Payment processing |
| `SendGrid` | Email sending |
| `OpenTelemetry` | Distributed tracing |
| `Azure.Identity` | Azure Key Vault auth |
| `xUnit`, `Moq`, `FluentAssertions` | Testing |

### Frontend NPM Packages

| Package | Role |
|---------|------|
| `@angular/core`, `@angular/common` | Core Angular framework |
| `@angular/router` | Routing |
| `@angular/forms` | Form handling |
| `rxjs` | Reactive extensions |
| `bootstrap` | UI framework |
| `@popperjs/core` | Tooltip/popover positioning |
| `chart.js`, `ng2-charts` | Charting |
| `ngx-toastr` | Toast notifications |
| `@stripe/stripe-js` | Stripe payments |
| `zone.js` | Angular change detection |
| `tslib` | TypeScript helpers |

---

## 9. Testing Strategy

### Backend Testing (xUnit)
- **Location:** `Ecommerce.Tests/`
- **Patterns:**
  - `ControllerTests` - API endpoint testing
  - `QueryHandlerTests` / `CommandHandlerTests` - CQRS handler testing
- **Tools:** xUnit, Moq, FluentAssertions, AutoFixture

### Frontend Testing (Karma/Jasmine)
- **Location:** Co-located with components (`.spec.ts` files)
- **Example:** `ecommerce-ui/src/app/login/login.component.spec.ts`
- **Run:** `npm test`

---

## 10. Infrastructure Services Configuration

### Docker Compose Services

| Service | Port | Purpose |
|---------|------|---------|
| `ecommerce-api` | 7273 | Backend API |
| `ecommerce-ui` | 4200 | Frontend |
| `sqlserver` | 1433 | SQL Server database |
| `redis` | 6379 | Redis cache |
| `rabbitmq` | 5672, 15672 | RabbitMQ + Management UI |
| `sonarqube` | 9000 | Code quality analysis |

### Environment Variables (`.env`)
- `SQL_SA_PASSWORD` - SQL Server password
- `JWT_SECRET_KEY` - JWT signing key
- `SONARQUBE_DB_*` - SonarQube database credentials
- `SONAR_HOST_URL` - SonarQube URL
- `SONAR_TOKEN` - SonarQube auth token

---

## 11. Key Security Features

### Authentication
- JWT Bearer tokens with 15-minute expiry
- Refresh tokens with 7-day expiry
- OAuth 2.0 integration (Microsoft, Google)
- Role-based authorization (Admin, User, Customer, WarehouseManager)

### Rate Limiting
- IP-based rate limiting (200 requests/minute general, 5/minute for login)
- Client-based rate limiting (60 requests/minute)
- Configurable via `appsettings.json`

### Security Headers
- `SecurityHeadersMiddleware` adds security headers to responses

---

## 12. Observability

### Logging (Serilog)
- Console output
- File output (`Logs/ECommerceLogs.txt`)
- Application Insights integration

### Tracing (OpenTelemetry)
- ASP.NET Core instrumentation
- HTTP client instrumentation
- SQL client instrumentation
- OTLP exporter support

### Monitoring
- System metrics stored in `SystemMetric` entity
- Error logging via `ErrorLog` entity
- API alerts via `ApiAlert` entity

---

*Analysis completed: 2026-03-23*
