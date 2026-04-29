# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.


## Instructions
** Try to be modular and stick to SOLID principles when writing code. 
** Single Responsibility, Open/Close, Interface segregation.
** Use relevant folder structures, folder structures and names for easy comprehension.
** 



## Build & Run

```bash
# Build the API
dotnet build BlackInkPaperAPIService/BlackInkPaperAPIService.csproj --configuration Release

# Build the Admin portal
dotnet build BlackInkPaperAdmin/BlackInkPaperAdmin.csproj

# Run all tests
dotnet test

# Run a specific test class
dotnet test BlackInkPaperAPIService.Tests/BlackInkPaperAPIService.Tests.csproj --filter ProductApplicationServiceTests

# Integration tests (require TEST_SQLSERVER_CONNECTION_STRING env var)
dotnet test BlackInkPaperAPIService.Tests/BlackInkPaperAPIService.Tests.csproj --filter Category=Integration
```

CI/CD deploys the API only (not Admin) to Azure Web App on push to `main`.

## Architecture

Five-project layered solution targeting .NET 9.0:

```
BlackInkPaperAPIService/   → ASP.NET Core API (controllers, middleware, DI setup)
BlackInkPaperAdmin/        → Blazor admin portal (calls API via AdminApi:BaseUrl)
Application/               → DTOs, service interfaces, application services
Domain/                    → Aggregate roots: Product, Cart, Order, ShippingAddress, ProductVariant
Infrastructure/            → Dapper repositories, EF Core Identity, external service clients
Common/                    → ServiceResponse<T> wrapper, paging models
BlackInkPaperAPIService.Tests/ → xUnit unit + integration tests
```

**Request flow:**
Controller → Application Service → Dapper Repository → PostgreSQL (Supabase)

All API responses are wrapped in `ServiceResponse<T>` (Common layer) with `Success`, `StatusCode`, `ErrorCode`, `Message`, and `Data` fields.

## Key Architectural Decisions

**Dapper + EF Core hybrid**: Dapper handles all business/query operations (performance); EF Core is used only for ASP.NET Identity tables (users, roles).

**Domain aggregates are plain data holders** — no business logic lives in them. All logic is in Application Services (e.g., `ProductApplicationService`, `CartApplicationService`).

**Token blacklisting middleware**: Logout invalidates JWTs by storing the JTI claim in a `TokenBlacklist` table. `TokenBlacklistMiddleware` checks every request.

**Admin API base URL** is configured in `BlackInkPaperAdmin/appsettings.json` under `AdminApi:BaseUrl` (default: `https://localhost:7023/`).

## Database

- **Primary**: PostgreSQL via Supabase — configured in `appsettings.json` as `ConnectionStrings:DefaultConnection`.
- **Fallback**: Azure SQL Server — `ConnectionStrings:DefaultConnectionSQLSERVER`.
- EF Core migrations live in `Infrastructure/Migrations/` (Identity tables only).
- Raw SQL for business queries is embedded in Dapper repositories under `Infrastructure/Repositories/`.

## Payments & Pricing

Razorpay is the payment gateway (Indian market). Webhook endpoint: `POST /api/razorpay/webhook`.

Shipping cost formula (India-specific, INR):
- Base: ₹99 flat + ₹25/item + ₹40/kg
- GST: 18%

Pricing config lives in `appsettings.json` under `CheckoutPricing`.

## Testing Conventions

- **Unit tests**: Use hand-written fakes (e.g., `FakeProductRepository`) — no mocking frameworks.
- **Integration tests**: Hit a real DB; require `TEST_SQLSERVER_CONNECTION_STRING`.
- **Test naming**: `MethodName_ExpectedBehavior_WhenCondition`
- Test data builders live in `BlackInkPaperAPIService.Tests/Products/ProductTestData.cs`.
