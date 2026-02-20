# Exception Handling - API Level Testing Guide

## 🎯 Quick Testing Checklist

Use these specific API endpoints to test exception handling:

---

## ✅ Test 1: Correlation ID Support

### Step 1: Start the Application
```bash
dotnet run --project Ecommerce.API/Ecommerce.API.csproj
```

### Step 2: Test with Swagger UI

1. Open: `https://localhost:7273/swagger`

2. **Test Invalid Endpoint:**
   - Try: `GET /api/this-endpoint-does-not-exist`
   - **Check Response Headers:** Look for `X-Correlation-ID`
   - **Check Response Body:** Look for `data.correlationId`

3. **Test with Custom Correlation ID:**
   - In Swagger, click "Authorize" (if available) or use browser dev tools
   - Add header: `X-Correlation-ID: test-12345`
   - Make any request that fails
   - **Verify:** Response header and body contain `test-12345`

### Step 3: Test with PowerShell

```powershell
# Test correlation ID
$headers = @{
    "X-Correlation-ID" = "my-test-correlation-id"
}

try {
    Invoke-WebRequest -Uri "https://localhost:7273/api/invalid-endpoint" `
        -Headers $headers `
        -ErrorAction Stop
} catch {
    $response = $_.Exception.Response
    Write-Host "Correlation ID in header: $($response.Headers['X-Correlation-ID'])" -ForegroundColor Green
    
    $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
    $body = $reader.ReadToEnd() | ConvertFrom-Json
    Write-Host "Correlation ID in body: $($body.data.correlationId)" -ForegroundColor Green
}
```

**Expected Result:**
- ✅ `X-Correlation-ID` header in response
- ✅ `data.correlationId` in response body
- ✅ Matches the value you sent (or new GUID if not sent)

---

## ✅ Test 2: Exception Mapping to HTTP Status Codes

### Test 2.1: Validation Error (400)

**Endpoint:** `POST /api/auth/register`

**Request:**
```json
{
  "email": "",
  "password": "",
  "firstName": "",
  "lastName": ""
}
```

**Expected:**
- HTTP Status: `400 Bad Request`
- Response:
```json
{
  "isSuccessful": false,
  "status": "ValidationError",
  "statusReason": "Invalid input provided...",
  "data": {
    "correlationId": "...",
    "timestamp": "..."
  }
}
```

### Test 2.2: Not Found (404)

**Endpoint:** `GET /api/products/999999999`

**Expected:**
- HTTP Status: `404 Not Found`
- Response:
```json
{
  "isSuccessful": false,
  "status": "NotFound",
  "statusReason": "The requested resource was not found.",
  "data": { ... }
}
```

### Test 2.3: Unauthorized (401)

**Endpoint:** `GET /api/orders/my-orders`

**Request:** No Authorization header

**Expected:**
- HTTP Status: `401 Unauthorized`
- Response:
```json
{
  "isSuccessful": false,
  "status": "Unauthorized",
  "statusReason": "Access denied. Please authenticate and try again.",
  "data": { ... }
}
```

### Test 2.4: Business Rule Violation (409)

**Endpoint:** `POST /api/orders/checkout`

**Request:** (with items that exceed stock)

**Expected:**
- HTTP Status: `409 Conflict`
- Response:
```json
{
  "isSuccessful": false,
  "status": "BusinessRuleViolation",
  "statusReason": "A business rule violation occurred...",
  "data": { ... }
}
```

---

## ✅ Test 3: ApiResponse Format Consistency

### Test Any Error Endpoint:

**Endpoint:** `GET /api/invalid-endpoint`

**Expected Response Structure:**
```json
{
  "isSuccessful": false,
  "status": "Exception",
  "statusReason": "An unexpected error occurred...",
  "data": {
    "correlationId": "guid-here",
    "timestamp": "2025-01-07T12:34:56.789Z"
  }
}
```

**Checks:**
- [ ] `isSuccessful` is `false`
- [ ] `status` is present (not empty)
- [ ] `statusReason` is present and user-friendly
- [ ] `data` object exists
- [ ] `data.correlationId` exists
- [ ] `data.timestamp` exists

---

## ✅ Test 4: Security - No Internal Details

### Test in Production Mode:

**Set Environment:**
```bash
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet run --project Ecommerce.API/Ecommerce.API.csproj
```

**Test Endpoint:** `GET /api/invalid-endpoint`

**Expected (Production):**
```json
{
  "isSuccessful": false,
  "status": "Exception",
  "statusReason": "An unexpected error occurred. Please try again or contact support if the issue persists.",
  "data": {
    "correlationId": "...",
    "timestamp": "..."
    // NO stackTrace
    // NO innerException
  }
}
```

**Checks:**
- [ ] `data.stackTrace` is NOT present
- [ ] `data.innerException` is NOT present
- [ ] `statusReason` does NOT contain technical terms

### Test in Development Mode:

**Set Environment:**
```bash
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project Ecommerce.API/Ecommerce.API.csproj
```

**Expected (Development):**
```json
{
  "isSuccessful": false,
  "status": "Exception",
  "statusReason": "Detailed error message",
  "data": {
    "correlationId": "...",
    "timestamp": "...",
    "stackTrace": "at ...", // OK in development
    "innerException": { ... } // OK in development
  }
}
```

**Checks:**
- [ ] `data.stackTrace` IS present (OK in dev)
- [ ] `data.innerException` may be present (OK in dev)

---

## 🧪 Using Postman Collection

### Create Postman Collection:

1. **Test Correlation ID:**
   ```
   GET https://localhost:7273/api/invalid-endpoint
   Headers:
     X-Correlation-ID: test-123
   ```

2. **Test Validation Error:**
   ```
   POST https://localhost:7273/api/auth/register
   Body (JSON):
   {
     "email": "",
     "password": ""
   }
   ```

3. **Test Unauthorized:**
   ```
   GET https://localhost:7273/api/orders/my-orders
   (No Authorization header)
   ```

---

## 📊 Quick Test Script

### PowerShell One-Liner Tests:

```powershell
# Test 1: Correlation ID
$headers = @{ "X-Correlation-ID" = "test-123" }
try { Invoke-WebRequest -Uri "https://localhost:7273/api/invalid" -Headers $headers } catch { 
    $_.Exception.Response.Headers["X-Correlation-ID"]
    ($_.Exception.Response.GetResponseStream() | ConvertFrom-Json).data.correlationId
}

# Test 2: Validation Error
try { 
    Invoke-WebRequest -Uri "https://localhost:7273/api/auth/register" `
        -Method POST `
        -Body '{"email":"","password":""}' `
        -ContentType "application/json"
} catch {
    $_.Exception.Response.StatusCode.value__
    ($_.Exception.Response.GetResponseStream() | ConvertFrom-Json).status
}

# Test 3: ApiResponse Format
try { Invoke-WebRequest -Uri "https://localhost:7273/api/invalid" } catch {
    $json = ($_.Exception.Response.GetResponseStream() | ConvertFrom-Json)
    Write-Host "isSuccessful: $($json.isSuccessful)"
    Write-Host "status: $($json.status)"
    Write-Host "hasCorrelationId: $($null -ne $json.data.correlationId)"
}
```

---

## ✅ Complete Test Checklist

### Correlation ID:
- [ ] Correlation ID in response header
- [ ] Correlation ID in response body
- [ ] Custom correlation ID preserved
- [ ] New correlation ID generated if not provided

### Exception Mapping:
- [ ] `ArgumentException` → 400
- [ ] `InvalidOperationException` → 409
- [ ] `KeyNotFoundException` → 404
- [ ] `UnauthorizedAccessException` → 401
- [ ] Default → 500

### ApiResponse Format:
- [ ] `isSuccessful` = false
- [ ] `status` present
- [ ] `statusReason` present
- [ ] `data` object exists
- [ ] `data.correlationId` exists
- [ ] `data.timestamp` exists

### Security:
- [ ] No stack trace in production
- [ ] No inner exception in production
- [ ] User-friendly error messages
- [ ] Stack trace OK in development

---

## 🎯 Success Criteria

All tests pass if:
1. ✅ Every error response includes correlation ID
2. ✅ Exceptions map to correct HTTP status codes
3. ✅ All errors follow ApiResponse format
4. ✅ No internal details exposed in production
5. ✅ Error messages are user-friendly

---

**Ready to test!** Start your application and run through these scenarios.

