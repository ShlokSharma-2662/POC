# Ecommerce API Test Suite

This test suite provides comprehensive xUnit test coverage for all controllers in the Ecommerce API project, targeting 90% code coverage.

## Test Structure

### Controllers Tested

1. **AuthController** - Authentication and user management
   - User registration
   - User login
   - Profile retrieval
   - Password change

2. **AdminController** - Administrative operations
   - Order management
   - User management
   - System metrics
   - Revenue reports

3. **ProductsController** - Product management
   - CRUD operations
   - Image handling
   - Admin operations

4. **CategoriesController** - Category management
   - Category retrieval

5. **CartController** - Shopping cart operations
   - Add/remove items
   - Update quantities
   - Clear cart

6. **WishlistController** - Wishlist management
   - Add/remove items
   - Check status
   - Clear wishlist

7. **PaymentsController** - Payment processing
   - Payment intent creation

8. **OAuthController** - OAuth authentication
   - Login flow
   - Callback handling
   - User info retrieval

9. **OrdersController** - Order management
   - Order checkout
   - Order retrieval

10. **BaseController** - Base controller functionality
    - Response helpers
    - Exception handling

## Test Coverage

Each controller test suite covers:

- **Happy Path Scenarios** - Valid inputs and expected successful responses
- **Validation Scenarios** - Invalid inputs and validation error responses
- **Authentication Scenarios** - Authorized and unauthorized access
- **Exception Scenarios** - Error handling and exception responses
- **Edge Cases** - Boundary conditions and special cases

## Running Tests

### Prerequisites

Ensure you have the following packages installed:
- Microsoft.NET.Test.Sdk
- xunit
- xunit.runner.visualstudio
- Moq
- Microsoft.AspNetCore.Mvc.Testing
- Microsoft.AspNetCore.TestHost
- Microsoft.EntityFrameworkCore.InMemory
- FluentAssertions
- AutoFixture
- AutoFixture.Xunit2
- AutoFixture.AutoMoq

### Command Line

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=AuthControllerTests"

# Run tests with detailed output
dotnet test --verbosity normal
```

### Visual Studio

1. Open the solution in Visual Studio
2. Go to Test → Test Explorer
3. Build the solution
4. Run all tests or specific test classes

## Test Data

Tests use:
- **Mock objects** for dependencies (MediatR, services, etc.)
- **AutoFixture** for generating test data
- **FluentAssertions** for readable assertions
- **In-memory database** for integration scenarios

## Code Coverage

The test suite is designed to achieve 90% code coverage across all controllers. Coverage includes:

- All public methods
- All conditional branches
- Exception handling paths
- Validation scenarios
- Authentication/authorization flows

## Test Categories

Tests are organized by:
- **Unit Tests** - Individual method testing with mocked dependencies
- **Integration Tests** - End-to-end scenarios with real dependencies
- **Exception Tests** - Error handling and edge cases
- **Validation Tests** - Input validation scenarios

## Best Practices

1. **Arrange-Act-Assert** pattern for test structure
2. **Descriptive test names** that explain the scenario
3. **Single responsibility** - one test per scenario
4. **Mock external dependencies** to isolate units under test
5. **Use FluentAssertions** for readable test assertions
6. **Test both success and failure scenarios**

## Maintenance

When adding new controllers or methods:

1. Create corresponding test classes
2. Follow the existing test patterns
3. Ensure 90% code coverage is maintained
4. Update this README with new test information

## Dependencies

The test project references:
- Ecommerce.API
- Ecommerce.Application
- Ecommerce.Infrastructure

All external dependencies are mocked to ensure fast, reliable, and isolated tests.

