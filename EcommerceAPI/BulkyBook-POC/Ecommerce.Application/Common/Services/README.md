# UserContextService

The `UserContextService` provides a centralized way to extract user information from JWT claims across your application.

## Features

- **GetCurrentUserId()**: Returns the current user ID as `long?` (nullable)
- **GetCurrentUserIdOrThrow()**: Returns the current user ID as `long` or throws an exception if not found
- **GetClaimValue(string claimType)**: Gets any specific claim value from the JWT token
- **IsAuthenticated()**: Checks if the current user is authenticated

## Usage Examples

### Basic Usage in Query/Command Handlers

```csharp
public class MyQueryHandler : IRequestHandler<MyQuery, MyResult>
{
    private readonly IUserContextService _userContextService;

    public MyQueryHandler(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    public async Task<MyResult> Handle(MyQuery request, CancellationToken cancellationToken)
    {
        // Get user ID (nullable approach)
        var userId = _userContextService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return new MyResult(); // Handle unauthenticated case
        }

        // Use userId.Value in your logic
        var data = await _context.MyData
            .Where(d => d.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        return new MyResult(data);
    }
}
```

### Using the Throw Method

```csharp
public async Task<MyResult> Handle(MyQuery request, CancellationToken cancellationToken)
{
    // This will throw UnauthorizedAccessException if user is not authenticated
    var userId = _userContextService.GetCurrentUserIdOrThrow();

    var data = await _context.MyData
        .Where(d => d.UserId == userId)
        .ToListAsync(cancellationToken);

    return new MyResult(data);
}
```

### Getting Other Claims

```csharp
public async Task<MyResult> Handle(MyQuery request, CancellationToken cancellationToken)
{
    var userEmail = _userContextService.GetClaimValue(ClaimTypes.Email);
    var userRole = _userContextService.GetClaimValue(ClaimTypes.Role);
    
    // Use the claim values in your logic
}
```

### Checking Authentication Status

```csharp
public async Task<MyResult> Handle(MyQuery request, CancellationToken cancellationToken)
{
    if (!_userContextService.IsAuthenticated())
    {
        return new MyResult(); // Handle unauthenticated case
    }

    var userId = _userContextService.GetCurrentUserIdOrThrow();
    // Continue with authenticated logic
}
```

## Supported Claim Types

The service automatically tries to find the user ID from these claim types in order:
1. `JwtRegisteredClaimNames.Sub` ("sub")
2. `ClaimTypes.NameIdentifier` ("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
3. Custom "uid" claim

## Registration

The service is already registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IUserContextService, UserContextService>();
```

## Benefits

1. **Centralized Logic**: All JWT claim extraction logic is in one place
2. **Consistent Error Handling**: Standardized approach to handling missing claims
3. **Easy Testing**: Can be easily mocked in unit tests
4. **Type Safety**: Returns strongly-typed `long` instead of string
5. **Flexible**: Supports both nullable and exception-throwing approaches
