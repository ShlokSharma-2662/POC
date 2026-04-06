

**PRODUCT REQUIREMENTS DOCUMENT**

**Scalable Enterprise Management System**

Advanced .NET Development  |  8+ Years Level

| Version | 1.0 |
| :---- | :---- |
| **Status** | Draft \- Ready for Development |
| **Track** | 8+ Years Experience POC |
| **Phases** | 5 Phases |
| **Total Estimated Hours** | \~72 Hours |

*Builds on the completed 4-8 Year track, introducing cloud-native, event-driven, and advanced API capabilities.*

# **1\. Product Overview**

## **1.1 Purpose**

This PRD defines requirements for the Scalable Enterprise Management System (SEMS) — a cloud-native, event-driven, API-first application built on advanced .NET technologies. It serves as the 8+ Year track POC and extends the concepts introduced in the 4-8 Year Order & Fulfillment System.

## **1.2 What Changes at 8+ Level**

The 4-8 Year POC established CQRS with MediatR, EF Core, OAuth 2.0, and a basic event-driven pipeline. This POC advances the architecture with:

* Azure Durable Functions for stateful, fault-tolerant workflow orchestration

* Azure Event Grid for decoupled, scalable inter-service communication

* GraphQL (Hot Chocolate) with subscriptions, OData alongside gRPC

* CosmosDB for event-sourced data persistence

* Advanced observability via Azure Monitor, Application Insights, and OpenTelemetry

* Redis distributed caching with explicit invalidation strategies

* Terraform IaC and AKS deployment replacing local Docker Compose

## **1.3 Goals**

* Deliver a production-grade, modular microservices system

* Demonstrate mastery of cloud-native .NET patterns at senior/principal level

* Showcase the full CQRS \+ event-sourcing \+ task orchestration lifecycle

* Integrate performance benchmarking and structured security enforcement

## **1.4 Non-Goals**

* External payment gateway integration (mocked in Phase 2\)

* Mobile client development

* Multi-region Active/Active deployment

# **2\. Stakeholders & User Personas**

| Persona | Role | Key Needs |
| :---- | :---- | :---- |
| Admin | Product & system management | Manage products, monitor performance, view logs |
| Customer | End-user of the storefront | Browse products, place orders, track shipment status |
| Warehouse Manager | Inventory operations | Real-time stock visibility, low-stock alerts |
| System Administrator | Platform health & security | API latency dashboards, error tracking, RBAC enforcement |
| Developer / Trainer | POC evaluation & mentorship | Clean code structure, concept coverage, reference links |

# **3\. Technology Stack**

| Component | Technology |
| :---- | :---- |
| Backend Runtime | .NET 8, ASP.NET Core |
| CQRS / Mediator | MediatR 12 |
| ORM | EF Core 8 with compiled queries |
| Transactional Database | SQL Server 2022 |
| Event Store / NoSQL | Azure CosmosDB (NoSQL API) |
| Distributed Cache | Redis (StackExchange.Redis) |
| Messaging | Azure Event Grid, RabbitMQ |
| Task Orchestration | Azure Functions v4, Azure Durable Functions |
| API Protocols | REST, gRPC (Grpc.AspNetCore), GraphQL (Hot Chocolate), OData |
| Authentication | OAuth 2.0, JWT, Azure AD / IdentityServer4 |
| Secret Management | Azure Key Vault \+ Managed Identity |
| Logging | Serilog \+ Azure Log Analytics sink |
| Monitoring / Tracing | Azure Monitor, Application Insights, OpenTelemetry |
| Frontend | Angular, React, .NET MVC |
| Containers | Docker (multi-stage), Kubernetes AKS |
| CI/CD | GitHub Actions \+ Azure DevOps |
| Infrastructure as Code | Terraform |

# **4\. System Architecture**

## **4.1 Microservices Map**

| Service | Responsibility | Protocols |
| :---- | :---- | :---- |
| Product Service | CRUD for products, stock management | REST \+ GraphQL \+ OData \+ Event Grid |
| Order Service | Order placement, lifecycle, fulfillment | REST \+ gRPC \+ Durable Functions |
| Notification Service | Email / SMS alerts for order events | Event Grid subscriber \+ Azure Functions |
| User Service | Auth, RBAC, token issuance | OAuth 2.0 / OIDC |
| Payment Service | Payment transaction coordination (mocked) | gRPC \+ Event Grid |
| Analytics Service | Real-time metrics aggregation | Application Insights \+ CosmosDB |

## **4.2 Data Stores**

| Store | Usage |
| :---- | :---- |
| SQL Server | Transactional writes: Orders, Products, Inventory, Users |
| Azure CosmosDB | Event sourcing: OrderEvents, StockMovements |
| Redis | Read-through cache: Product catalog, order history queries |
| Azure Blob / Queue Storage | Notification backlog, file attachments, dead-letters |

## **4.3 Core Event Flow**

* Customer places order \-\> OrderPlaced event published to Event Grid

* Inventory Service subscribes \-\> decrements stock, publishes StockUpdated

* Azure Durable Function orchestrator tracks order state machine with retries

* Notification Service subscribes \-\> sends confirmation email/SMS via Azure Functions

* On shipment: OrderShipped event published \-\> tracking dashboard updated via GraphQL subscription

# **5\. Phase 1 \- Product & Inventory Management**

**Phase 1: Product & Inventory Management**  |  Est. 16 hours

## **5.1 Objectives**

* Implement a standalone Product microservice with full CRUD

* Integrate Redis caching (cache-aside pattern) with invalidation on update/delete

* Trigger Azure Function on product change to publish ProductUpdated to Event Grid

* Expose product data via REST, GraphQL (Hot Chocolate), and OData

## **5.2 User Stories**

**User Story 1: Admin can add, update, and remove products**

**Acceptance Criteria:**

* Product entity: Name, Category, Price, StockStatus, StockQuantity, ImageURL, Description

* Stock availability reflected system-wide within 500ms of an update

* Azure Function triggered on any product mutation \-\> publishes ProductUpdated to Event Grid

* Redis cache invalidated on update/delete; 5-minute TTL on reads

* EF Core compiled queries used for all product search operations

**Key Concepts:**

* Microservices: Product Service deployed as an independent project

* Azure Function for Event Processing: listens for DB mutations, triggers Event Grid

* Distributed Caching (Redis): cache-aside pattern with TTL and manual invalidation

* EF Core Compiled Queries: EF.CompileAsyncQuery\<T\> for repeated search expressions

**User Story 2: Customer can browse and search products efficiently**

**Acceptance Criteria:**

* Advanced search with filters: category, price range, stock availability

* OData query support on /api/products ($filter, $orderby, $top, $skip, $count)

* GraphQL endpoint for flexible field selection queries

* API rate limiting: 100 req/min per client IP on product search endpoints

* p95 search response time \< 200ms with cache warm

**Key Concepts:**

* GraphQL with Hot Chocolate: schema-first query layer, DataLoader for batching

* OData on ASP.NET Core: \[EnableQuery\] attribute on IQueryable\<Product\>

* ASP.NET Core RateLimiter middleware: fixed window policy

* Composite DB indexes on (Category, Price) for filtered LINQ queries

## **5.3 API Endpoints \- Product Service**

| Method | Endpoint | Required Role | Description |
| :---- | :---- | :---- | :---- |
| GET | /api/products | Any authenticated | Fetch all products (OData supported) |
| GET | /api/products/{id} | Any authenticated | Fetch single product |
| POST | /api/products | Admin | Create a new product |
| PUT | /api/products/{id} | Admin | Update product \+ invalidate Redis cache |
| DELETE | /api/products/{id} | Admin | Soft-delete product (IsDeleted \= true) |
| POST | /graphql | Any authenticated | GraphQL query / mutation interface |

## **5.4 Database Schema \- Products Table**

| Column | Type & Constraint |
| :---- | :---- |
| ProductId (PK) | UNIQUEIDENTIFIER, NOT NULL, DEFAULT NEWSEQUENTIALID() |
| Name | NVARCHAR(200), NOT NULL |
| Category | NVARCHAR(100), NOT NULL, INDEX |
| Price | DECIMAL(18,2), NOT NULL, INDEX |
| StockStatus | NVARCHAR(50), NOT NULL |
| StockQuantity | INT, DEFAULT 0 |
| ImageURL | NVARCHAR(500), NULL |
| Description | NVARCHAR(MAX), NULL |
| CreatedAt | DATETIME2, DEFAULT GETUTCDATE() |
| UpdatedAt | DATETIME2, NULL |
| IsDeleted | BIT, DEFAULT 0 |

## **5.5 Reference Resources**

* Azure Functions Overview: https://www.youtube.com/watch?v=Hb5QPFWm8i4

* EF Core Optimization: https://www.youtube.com/watch?v=25H84BXcr9M

* GraphQL in .NET: https://www.youtube.com/watch?v=vN\_j1Bs0ALU

* OData in .NET: https://www.youtube.com/watch?v=191CJFrvBxM

# **6\. Phase 2 \- Order Processing & Automation**

**Phase 2: Order Processing & Automation**  |  Est. 18 hours

## **6.1 Objectives**

* Implement CQRS-based Order Service using MediatR command/query handlers

* Use Azure Durable Functions to orchestrate the full order state machine

* Publish domain events (OrderPlaced, OrderShipped, OrderDelivered) to Event Grid

* Ensure transactional consistency via EF Core transactions \+ Unit of Work pattern

## **6.2 User Stories**

**User Story 1: Customer can place an order and track its status**

**Acceptance Criteria:**

* Order lifecycle: Pending \-\> Confirmed \-\> Shipped \-\> Delivered

* Each status change publishes a domain event to Azure Event Grid

* Azure Durable Function orchestrator manages transitions with automatic retry

* CQRS separation: PlaceOrderCommand handled via command handler, GetOrderQuery via query handler

* EF Core transaction wraps order creation \+ inventory decrement atomically

* Order status queryable within 1 second of any state change

**Key Concepts:**

* CQRS with MediatR: PlaceOrderCommand, UpdateOrderStatusCommand, GetOrderByIdQuery

* Event-Driven Architecture: OrderPlaced, OrderShipped events published to Event Grid

* Azure Durable Functions: Orchestrator \+ Activity function pattern for order workflow

* EF Core Transaction Management: BeginTransactionAsync for atomic multi-table writes

**User Story 2: Customer receives notifications for each order status update**

**Acceptance Criteria:**

* Email/SMS triggered on: OrderConfirmed, OrderShipped, OrderDelivered events

* Notification Service subscribes to Event Grid topic via Azure Function webhook trigger

* Retry policy: 3 attempts with exponential backoff on notification failure

* Dead-letter queue captures all permanently failed notifications

* Notification log persisted to CosmosDB for audit trail

**Key Concepts:**

* Azure Event Grid subscriber: HTTP webhook endpoint or Azure Function trigger

* Azure Functions: notification dispatch using SendGrid / Twilio

* Dead-letter storage: Azure Storage Queue for failed event replay

## **6.3 Order State Machine**

| From State | To State | Trigger / Event Published |
| :---- | :---- | :---- |
| (new) | Pending | PlaceOrderCommand executed successfully |
| Pending | Confirmed | Payment verified \-\> OrderConfirmed event |
| Confirmed | Shipped | Warehouse dispatch \-\> OrderShipped event |
| Shipped | Delivered | Delivery confirmation \-\> OrderDelivered event |
| Any | Cancelled | CancelOrderCommand \-\> OrderCancelled event |

## **6.4 API Endpoints \- Order Service**

| Method | Endpoint | Required Role | Description |
| :---- | :---- | :---- | :---- |
| POST | /api/orders | Customer | Place a new order |
| GET | /api/orders/{id} | Owner or Admin | Get order details |
| GET | /api/orders/customer/{customerId} | Owner or Admin | Customer order history |
| PUT | /api/orders/{id}/update-status | Admin | Manually update order status |
| DELETE | /api/orders/{id}/cancel | Owner or Admin | Cancel order |

## **6.5 Database Schema \- Order Module**

| Table.Column | Type & Constraint |
| :---- | :---- |
| Orders.OrderId (PK) | UNIQUEIDENTIFIER, NOT NULL |
| Orders.CustomerId (FK) | UNIQUEIDENTIFIER, NOT NULL, INDEX |
| Orders.Status | NVARCHAR(50), NOT NULL |
| Orders.TotalAmount | DECIMAL(18,2), NOT NULL |
| Orders.CreatedAt | DATETIME2, DEFAULT GETUTCDATE() |
| OrderItems.OrderItemId (PK) | UNIQUEIDENTIFIER, NOT NULL |
| OrderItems.OrderId (FK) | UNIQUEIDENTIFIER, NOT NULL |
| OrderItems.ProductId (FK) | UNIQUEIDENTIFIER, NOT NULL |
| OrderItems.Quantity | INT, NOT NULL |
| OrderItems.Price | DECIMAL(18,2), NOT NULL |
| OrderEvents (CosmosDB) | OrderId (partition key), EventType, Payload (JSON), Timestamp |

## **6.6 Reference Resources**

* CQRS Pattern: https://www.youtube.com/watch?v=h4KIngWVpfU

* Azure Durable Functions: https://www.youtube.com/watch?v=GQAgh\_z1rHY

# **7\. Phase 3 \- Performance Monitoring & Security**

**Phase 3: Performance Monitoring & Security**  |  Est. 18 hours

## **7.1 Objectives**

* Implement structured logging across all services using Serilog

* Integrate Azure Monitor and Application Insights for request telemetry

* Enforce OAuth 2.0 \+ RBAC across all API endpoints

* Add API rate limiting, FluentValidation pipeline behaviour, and TLS enforcement

* Store all secrets in Azure Key Vault accessed via Managed Identity

## **7.2 User Stories**

**User Story 1: System Admin can monitor API performance and security metrics**

**Acceptance Criteria:**

* All API requests log: endpoint, duration, HTTP status, user identity (structured JSON)

* Application Insights tracks p50/p95/p99 latency per endpoint

* Alert fires when p95 latency \> 500ms for any service over a 5-minute window

* Database query duration tracked separately via EF Core custom telemetry

* Error rate dashboard visible in Azure Monitor workbooks

**Key Concepts:**

* Serilog with Azure Log Analytics sink: structured, queryable logs via KQL

* Application Insights SDK: TrackDependency, TrackRequest, TrackException

* OpenTelemetry distributed tracing: correlate requests across microservices via trace IDs

**User Story 2: All API endpoints are secured with role-based access control**

**Acceptance Criteria:**

* JWT Bearer token required on all protected endpoints; 401 returned on missing/invalid token

* Roles: Admin, Customer, WarehouseManager enforced via policy-based authorization

* Token issued by Azure AD / IdentityServer4 with 15-minute expiry \+ refresh token

* Rate limiting: 60 req/min per user on write endpoints, 200 req/min on reads

* All command/request DTOs validated via FluentValidation in MediatR pipeline behaviour

* TLS 1.2+ enforced; HTTP redirected to HTTPS at AKS ingress level

**Key Concepts:**

* OAuth 2.0 / OIDC: Authorization Code Flow with PKCE for SPA clients

* ASP.NET Core policy-based authorization: resource-level policies beyond role checks

* Azure Key Vault references: no connection strings or secrets in appsettings.json

* MediatR pipeline behaviour: ValidationBehavior\<TRequest, TResponse\> runs before handler

## **7.3 Logging Strategy**

| Log Level | Use Case | Sink / Destination |
| :---- | :---- | :---- |
| Information | Request in/out, order state transitions | Azure Log Analytics |
| Warning | Slow queries (\>200ms), cache misses, retry attempts | Azure Log Analytics |
| Error | Unhandled exceptions, failed Event Grid delivery | Application Insights \+ Alert |
| Debug | Detailed EF Core SQL, handler internals | Console (dev environment only) |

## **7.4 Security Checklist**

| Area | Requirement |
| :---- | :---- |
| Authentication | OAuth 2.0 \+ JWT on all non-public endpoints |
| Authorization | RBAC at controller and MediatR handler level |
| Transport | HTTPS-only; HSTS header enabled; TLS 1.2+ minimum |
| Input Validation | FluentValidation in pipeline; EF Core parameterised queries |
| XSS Prevention | Output encoding \+ Content-Security-Policy header |
| Secret Management | Azure Key Vault via Managed Identity; no plaintext secrets |
| Dependency Scanning | GitHub Dependabot or OWASP Dependency-Check in CI |

## **7.5 Reference Resources**

* Application Insights: https://www.youtube.com/watch?v=w7yDuoCLVvQ

* Serilog for Logging: https://www.youtube.com/watch?v=nVAkSBpsuTk

# **8\. Phase 4 \- Advanced API Development**

**Phase 4: Advanced API Development**  |  Est. \~10 hours

## **8.1 Objectives**

* Add gRPC for service-to-service communication between Order and Product Services

* Expose GraphQL subscriptions for real-time order status on the React frontend

* Apply OData on query endpoints for admin dashboards with ad-hoc filtering

* Implement API versioning strategy (URL path versioning as primary)

## **8.2 User Stories**

**User Story 1: Services communicate efficiently over gRPC internally**

**Acceptance Criteria:**

* Order Service calls Product Service via gRPC to validate stock before order placement

* Protobuf contracts versioned and stored in a /protos shared directory

* Polly circuit breaker wraps gRPC calls with retry (3x) \+ half-open circuit

* gRPC response time \< 50ms on same-cluster calls (AKS pod-to-pod)

**Key Concepts:**

* Grpc.AspNetCore: server setup with AddGrpc(), client with GrpcChannel.ForAddress()

* Protobuf schema: strongly typed contracts with versioning via package declarations

* Polly v8 resilience pipeline: retry strategy \+ circuit breaker strategy

**User Story 2: Frontend receives real-time order status via GraphQL subscriptions**

**Acceptance Criteria:**

* GraphQL subscription event fires on every order status change

* React dashboard subscribes and updates order status badge without polling

* WebSocket transport via Hot Chocolate subscription middleware

* Subscription connection requires valid JWT; anonymous connections rejected with 4401

**Key Concepts:**

* Hot Chocolate subscriptions: ITopicEventSender.SendAsync() on state change

* Redis pub/sub as subscription transport for multi-instance deployments

* WebSocket authentication middleware: validate JWT from query param on upgrade

## **8.3 API Protocol Decision Matrix**

| Protocol | Used For | Client Audience | Key Reason |
| :---- | :---- | :---- | :---- |
| REST | External CRUD operations | Browser, mobile, third-party | Ubiquitous, easy to consume, tooling support |
| gRPC | Service-to-service calls | Internal microservices only | Strongly typed, low latency, streaming support |
| GraphQL | Flexible product/order reads | React SPA | Field selection, subscriptions, reduce over-fetching |
| OData | Paginated, filtered queries | Admin Angular dashboard | Ad-hoc querying without custom endpoints |

# **9\. Phase 5 \- Cloud-Based Task Automation**

**Phase 5: Cloud-Based Task Automation**  |  Est. \~10 hours

## **9.1 Objectives**

* Implement timer-triggered Azure Function for low-stock replenishment checks

* Use Durable Function fan-out/fan-in pattern for parallel batch order processing

* Add Event Grid dead-letter handling with configured retry policies

* Deploy all services to AKS with Terraform-managed Azure infrastructure

## **9.2 User Stories**

**User Story 1: System auto-triggers stock replenishment when inventory drops below threshold**

**Acceptance Criteria:**

* Timer-triggered Azure Function runs every 15 minutes via NCRONTAB schedule

* Checks all products where StockQuantity \< ReorderThreshold (configurable per product)

* Publishes StockReplenishmentRequired event to Event Grid custom topic

* Admin receives alert notification; replenishment request logged to CosmosDB

**Key Concepts:**

* Timer-triggered Azure Function: \[TimerTrigger("0 \*/15 \* \* \* \*")\] attribute

* Dependency injection in Azure Functions: IServiceCollection registration in Program.cs

* Azure Event Grid custom topic: define StockReplenishmentRequired event schema

**User Story 2: Durable Function orchestrator handles parallel batch order fulfillment**

**Acceptance Criteria:**

* Orchestrator function spins up parallel activity functions for each order item

* Fan-out/fan-in: each item stock validated concurrently via Task.WhenAll(activityTasks)

* Orchestrator waits for all activities before transitioning order to Confirmed

* Supports external event pattern for manual admin approval on flagged orders

* Orchestrator state checkpointed via Durable Task Framework for fault tolerance

**Key Concepts:**

* Azure Durable Functions: Orchestrator \+ Activity \+ External Event patterns

* Fan-out/fan-in: context.CallActivityAsync per item, awaited with Task.WhenAll

* Durable Entity: order state checkpointing across function restarts

## **9.3 Azure Functions Summary**

| Function Name | Trigger Type | Responsibility |
| :---- | :---- | :---- |
| ProductUpdatedProcessor | Event Grid | Cache invalidation \+ downstream notification dispatch |
| StockReplenishmentCheck | Timer (every 15 min) | Low-stock detection \+ replenishment event publish |
| OrderFulfillmentOrchestrator | HTTP or Queue | Durable orchestrator for full order lifecycle |
| NotificationDispatcher | Event Grid | Email/SMS dispatch on all order status events |
| DeadLetterProcessor | Azure Storage Queue | Retry or escalate permanently failed events |

## **9.4 Deployment Architecture**

* All microservices containerised with Docker using multi-stage builds for minimal image size

* Deployed to Azure Kubernetes Service (AKS) with separate namespace per microservice

* Terraform manages: AKS cluster, CosmosDB account, Redis Cache, Key Vault, Event Grid topics

* GitHub Actions CI/CD pipeline: test \-\> build \-\> push to Azure Container Registry \-\> deploy via Helm

* Azure DevOps pipeline available as an enterprise alternative CI/CD path

# **10\. Phased Delivery Summary**

| Phase | Name | Est. Hours | Key Deliverable |
| :---- | :---- | :---- | :---- |
| Phase 1 | Product & Inventory Management | 16 hrs | Product microservice, Redis cache, GraphQL/OData, Azure Functions trigger |
| Phase 2 | Order Processing & Automation | 18 hrs | CQRS order flow, Durable Function orchestrator, Event Grid domain events |
| Phase 3 | Performance Monitoring & Security | 18 hrs | Serilog, App Insights, OAuth 2.0 RBAC, Key Vault, rate limiting |
| Phase 4 | Advanced API Development | \~10 hrs | gRPC inter-service, GraphQL subscriptions, OData, API versioning |
| Phase 5 | Cloud-Based Task Automation | \~10 hrs | Scheduled functions, fan-out orchestration, AKS \+ Terraform deploy |

## **10.1 Key Differentiators from 4-8 Year POC**

| Capability | 4-8 Year POC | 8+ Year Addition |
| :---- | :---- | :---- |
| Event Processing | In-process domain events | Azure Event Grid (decoupled, scalable) |
| Workflow Orchestration | Manual status updates | Azure Durable Functions (stateful, fault-tolerant) |
| API Layer | REST \+ basic gRPC | gRPC \+ GraphQL subscriptions \+ OData |
| Data Layer | SQL Server only | CosmosDB event store added alongside SQL Server |
| Caching | IMemoryCache (in-process) | Redis distributed cache with explicit invalidation |
| Observability | Basic Serilog logging | OpenTelemetry \+ App Insights distributed tracing |
| Infrastructure | Docker Compose (local) | Terraform IaC \+ AKS (production-grade) |

## **10.2 Recommended Development Order**

* Start Phase 1 with the Product Service scaffold as an independent .NET Web API project

* Integrate Serilog and Application Insights at Phase 1 start (observability from day one)

* Define gRPC .proto contracts before Phase 2 inter-service calls (avoids rework)

* Complete Phase 2 before Phase 5 since Durable Functions build on the order event model

* Phase 3 security items (Key Vault, RBAC, rate limiting) can be layered in incrementally

# **11\. Definition of Done**

Each phase is considered complete when all of the following criteria are met:

## **11.1 Code Quality**

* All C\# code follows Microsoft naming conventions and passes dotnet format

* No hardcoded connection strings, API keys, or secrets anywhere in source code

* FluentValidation validators present for all MediatR command and request DTOs

* Unit tests cover all command handlers and query handlers (80%+ coverage target)

## **11.2 Functional**

* All user story acceptance criteria verified via Postman collection or integration tests

* Event Grid events confirmed delivered via Event Grid Viewer or dead-letter inspection

* Durable Function orchestration verified with replay log inspection

## **11.3 Performance**

* Product search p95 response time \< 200ms with Redis cache warm

* Order placement end-to-end \< 1 second (excluding external notification delivery)

* No N+1 queries: verified via EF Core diagnostic logging in development mode

## **11.4 Security**

* All protected endpoints return HTTP 401 for missing or invalid JWT

* Role violations return HTTP 403 (not 404 or 500\)

* Azure Key Vault references functional in deployed AKS environment

## **11.5 Observability**

* Application Insights shows correlated request traces across all services

* Serilog logs queryable in Azure Log Analytics workspace using KQL

* At least one custom metric tracked per service (e.g., orders\_placed\_total, cache\_hit\_ratio)

# **12\. Risks and Mitigation**

This POC introduces several advanced architectural components. The following identifies key risks and their mitigation strategies:

| Risk Area | Risk | Mitigation Strategy |
| :---: | :---- | :---- |
| **Complexity** | Integration complexity between gRPC, GraphQL, OData, and REST endpoints. | Define a clear, single API Gateway layer (e.g., Ocelot/YARP) early in Phase 1\. Use shared Protobuf/DTO contracts across all protocols. |
| **Data Consistency** | Maintaining eventual consistency between SQL Server (transactional) and CosmosDB (event store) following event failures. | Implement robust dead-letter queue (DLQ) handling and replay mechanisms (Phase 5). Ensure all state changes are idempotent. |
| **Orchestration** | Failure/escalation handling within complex Azure Durable Function workflows. | Design short, single-purpose activity functions. Implement external event handling for manual admin intervention on flagged workflow instances (Phase 5). |
| **Cloud Costs** | Uncontrolled consumption due to multiple Azure services (AKS, CosmosDB, Event Grid, Functions, Redis). | Use Terraform to define strict resource limits and SKUs. Implement Azure Policy to prevent deployment of high-cost tiers. Monitor costs via Azure Monitor billing integration (Phase 3). |

* 