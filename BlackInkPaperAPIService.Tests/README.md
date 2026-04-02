# BlackInkPaperAPIService Tests

This test project currently focuses on `ProductApplicationService`.

## What Is Here

- `Products/ProductApplicationServiceTests.cs`
  - Unit tests for product service behavior.
- `Products/Fakes/FakeProductRepository.cs`
  - A hand-written fake repository used by the service tests.
- `Products/ProductTestData.cs`
  - Shared test input and aggregate builders.
- `Products/ProductRepositoryIntegrationTests.cs`
  - Placeholder examples for future repository integration tests.

## Unit Tests vs Integration Tests

Use unit tests for service logic:
- validation
- not-found handling
- mapping behavior
- paging normalization
- response shape

Use integration tests for Dapper repositories:
- SQL correctness
- real database schema compatibility
- insert/update/delete behavior
- joins and search filters

Reason:
- a Dapper repository is mostly SQL, so the real risk is whether the SQL works against the database.

## Arrange / Act / Assert

Most tests should follow this structure.

### 1. Arrange

Set up the inputs, fake dependencies, and the system under test.

Example:

```csharp
var repository = new FakeProductRepository
{
    ExistsBySlugHandler = (_, _) => Task.FromResult(true)
};
var service = new ProductApplicationService(repository);
var request = ProductTestData.CreateRequest();
```

### 2. Act

Call the method you want to test.

```csharp
var response = await service.CreateAsync(request);
```

### 3. Assert

Check the result and any important side effects.

```csharp
Assert.False(response.Success);
Assert.Contains("Slug", response.Message);
Assert.Null(repository.AddedProduct);
```

## How To Think About A Test

A good unit test should answer one question clearly.

Examples:
- What happens if the product does not exist?
- What happens if the slug already exists?
- What happens when create is valid?
- Does update map the request into the aggregate correctly?

If a test tries to verify too many things at once, split it.

## Naming Style

The current tests use:

`MethodName_ExpectedBehavior_WhenCondition`

Examples:
- `GetByIdAsync_ReturnsFailure_WhenProductDoesNotExist`
- `CreateAsync_ReturnsCreatedProduct_WhenRequestIsValid`

This makes failures easy to understand.

## How To Run Tests

Run all tests:

```bash
dotnet test BlackInkPaperAPIService.Tests/BlackInkPaperAPIService.Tests.csproj
```

Run only product service tests:

```bash
dotnet test BlackInkPaperAPIService.Tests/BlackInkPaperAPIService.Tests.csproj --filter ProductApplicationServiceTests
```

Run from the solution root:

```bash
dotnet test
```

## Running In Rider Or Visual Studio

- Open the solution.
- Open the test file or the test project.
- Use the gutter run icon next to a test or test class.
- Or run all tests from the test explorer.

## Adding A New Service Test

Use this workflow:

1. Add or reuse input data from `ProductTestData`.
2. Configure `FakeProductRepository` for the scenario.
3. Create `ProductApplicationService`.
4. Call the method.
5. Assert on `ServiceResponse<T>` and repository side effects.

## Adding Repository Tests Later

Repository tests should use:
- a dedicated test database
- known seeded data
- cleanup between tests

Do not treat repository SQL tests as pure unit tests.

## Repository Integration Test Setup

The repository integration tests use a real SQL Server connection and require this environment variable:

```bash
TEST_SQLSERVER_CONNECTION_STRING="Server=...;Initial Catalog=...;User ID=...;Password=...;Encrypt=True;"
```

Then run:

```bash
dotnet test BlackInkPaperAPIService.Tests/BlackInkPaperAPIService.Tests.csproj --filter Category=Integration
```

The current repository integration tests:
- clone foreign key values from an existing `Products` row
- insert an isolated test product with unique identifiers
- verify repository behavior
- clean up the inserted row and child rows

If the environment variable is not set, those tests return immediately.

## First Practical Goal

When adding new product features, start by adding service tests for:
- validation failures
- not found cases
- successful create/update/delete
- search behavior

Then add integration tests only for repository methods whose SQL changed.
