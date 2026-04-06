# Exception Handling Enhancement - API Testing Guide

## 🧪 Testing Overview

This guide helps you test the exception handling enhancements at the API level, including:
- ✅ Global exception handler middleware
- ✅ Correlation ID support
- ✅ Exception mapping to HTTP status codes
- ✅ ApiResponse format consistency
- ✅ Security (no internal details exposed)

## 📋 Prerequisites

1. **Application Running:**
   ```bash
   dotnet run --project Ecommerce.API/Ecommerce.API.csproj
   ```

2. **Test Tools:**
   - PowerShell (for test script)
   - Postman, curl, or Swagger UI (for manual testing)

---

## 🚀 Quick Start

### Run All Tests:
```powershell
.\scripts\test-exception-handling.ps1 -All
```

### Run Specific Tests:
```powershell
# Test correlation IDs
.\scripts\test-exception-handling.ps1 -TestCorrelationId

# Test exception mapping
.\scripts\test-exception-handling.ps1 -TestExceptionMapping

# Test ApiResponse format
.\scripts\test-exception-handling.ps1 -TestApiResponse

# Test security
.\scripts\test-exception-handling.ps1 -TestSecurity
```

---

## 🧪 Test Scenarios

### Test 1: Correlation ID Support

**Objective:** Verify that correlation IDs are included in error responses.

**Manual Test:**
```http
GET /api/invalid-endpoint
X-Correlation-ID: test-correlation-id-123
```

**Expected:**
- ✅ Response header: `X-Correlation-ID: test-correlation-id-123`
- ✅ Response body: `{ "data": { "correlationId": "test-correlation-id-123", ... } }`

**Using PowerShell:**
```powershell
$headers = @{ "X-Correlation-ID" = "test-123" }
Invoke-WebRequest -Uri "https://localhost:7273/api/invalid-endpoint" -Headers $headers
```

**Check Response:**
- Look for `X-Correlation-ID` in response headers
- Check `data.correlationId` in response body

---

### Test 2: Exception Mapping to HTTP Status Codes

**Objective:** Verify exceptions are mapped to correct HTTP status codes.

#### Test 2.1: Validation Error (400)
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "",
  "password": ""
}
```

**Expected:**
- ✅ HTTP Status: `400 Bad Request`
- ✅ Response: `{ "status": "ValidationError", ... }`

#### Test 2.2: Not Found (404)
```http
GET /api/products/999999
```

**Expected:**
- ✅ HTTP Status: `404 Not Found`
- ✅ Response: `{ "status": "NotFound", ... }`

#### Test 2.3: Unauthorized (401)
```http
GET /api/orders/my-orders
(No Authorization header)
```

**Expected:**
- ✅ HTTP Status: `401 Unauthorized`
- ✅ Response: `{ "status": "Unauthorized", ... }`

#### Test 2.4: Business Rule Violation (409)
```http
POST /api/orders/checkout
(With insufficient stock)
```

**Expected:**
- ✅ HTTP Status: `409 Conflict`
- ✅ Response: `{ "status": "BusinessRuleViolation", ... }`

#### Test 2.5: Service Unavailable (503)
```http
POST /api/orders/checkout
(When circuit breaker is open)
```

**Expected:**
- ✅ HTTP Status: `503 Service Unavailable`
- ✅ Response: `{ "status": "ServiceUnavailable", ... }`

---

### Test 3: ApiResponse Format Consistency

**Objective:** Verify all error responses follow the ApiResponse format.

**Test Any Error:**
```http
GET /api/invalid-endpoint
```

**Expected Response Structure:**
```json
{
  "isSuccessful": false,
  "status": "Exception",
  "statusReason": "User-friendly error message",
  "data": {
    "correlationId": "guid",
    "timestamp": "2025-01-07T12:34:56.789Z",
    "stackTrace": "..." // only in development
  }
}
```

**Checks:**
- ✅ `isSuccessful` is `false`
- ✅ `status` is present and meaningful
- ✅ `statusReason` is user-friendly
- ✅ `data` object exists
- ✅ `data.correlationId` exists
- ✅ `data.timestamp` exists

---

### Test 4: Security - No Internal Details Exposed

**Objective:** Verify internal exception details are not exposed in production.

**Test:**
```http
GET /api/invalid-endpoint
```

**Checks:**

1. **Stack Trace:**
   - ❌ Should NOT be in production
   - ✅ OK in development

2. **Inner Exception:**
   - ❌ Should NOT be in production
   - ✅ OK in development

3. **Status Reason:**
   - ✅ Should be user-friendly
   - ❌ Should NOT contain technical terms like "System.Exception", "at ", "StackTrace"

**Expected (Production):**
```json
{
  "isSuccessful": false,
  "status": "Exception",
  "statusReason": "An unexpected error occurred. Please try again or contact support if the issue persists.",
  "data": {
    "correlationId": "guid",
    "timestamp": "2025-01-07T12:34:56.789Z"
    // NO stackTrace
    // NO innerException
  }
}
```

**Expected (Development):**
```json
{
  "isSuccessful": false,
  "status": "Exception",
  "statusReason": "Detailed error message",
  "data": {
    "correlationId": "guid",
    "timestamp": "2025-01-07T12:34:56.789Z",
    "stackTrace": "at ...", // OK in development
    "innerException": { ... } // OK in development
  }
}
```

---

## 🧪 Using Swagger UI

### Step 1: Open Swagger
Navigate to: `https://localhost:7273/swagger`

### Step 2: Test Endpoints

1. **Test Invalid Endpoint:**
   - Try: `GET /api/invalid-endpoint`
   - Check response format
   - Check correlation ID

2. **Test Validation Error:**
   - Try: `POST /api/auth/register` with empty email
   - Check status code (should be 400)
   - Check status text (should be "ValidationError")

3. **Test Unauthorized:**
   - Try: `GET /api/orders/my-orders` without authorization
   - Check status code (should be 401)
   - Check status text (should be "Unauthorized")

### Step 3: Check Response Headers
- Look for `X-Correlation-ID` in response headers
- Verify it's included in every error response

---

## 📊 Test Checklist

### Correlation ID:
- [ ] Correlation ID in response header (`X-Correlation-ID`)
- [ ] Correlation ID in response body (`data.correlationId`)
- [ ] Custom correlation ID from request is preserved
- [ ] New correlation ID generated if not provided

### Exception Mapping:
- [ ] `ArgumentException` → 400 Bad Request
- [ ] `InvalidOperationException` → 409 Conflict
- [ ] `KeyNotFoundException` → 404 Not Found
- [ ] `UnauthorizedAccessException` → 401 Unauthorized
- [ ] `TimeoutException` → 504 Gateway Timeout
- [ ] `BrokenCircuitException` → 503 Service Unavailable
- [ ] Default exceptions → 500 Internal Server Error

### ApiResponse Format:
- [ ] `isSuccessful` is `false` for errors
- [ ] `status` is present and meaningful
- [ ] `statusReason` is user-friendly
- [ ] `data` object exists
- [ ] `data.correlationId` exists
- [ ] `data.timestamp` exists

### Security:
- [ ] Stack trace NOT exposed in production
- [ ] Inner exception NOT exposed in production
- [ ] Status reason is user-friendly (no technical terms)
- [ ] Stack trace OK in development
- [ ] Inner exception OK in development

---

## 🔍 Manual Testing Examples

### Example 1: Test with curl

```bash
# Test correlation ID
curl -H "X-Correlation-ID: test-123" \
     -H "Content-Type: application/json" \
     https://localhost:7273/api/invalid-endpoint

# Test validation error
curl -X POST https://localhost:7273/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{"email":"","password":""}'

# Test unauthorized
curl https://localhost:7273/api/orders/my-orders
```

### Example 2: Test with Postman

1. **Create Request:**
   - Method: `GET`
   - URL: `https://localhost:7273/api/invalid-endpoint`

2. **Add Header:**
   - Key: `X-Correlation-ID`
   - Value: `test-correlation-id`

3. **Send Request**

4. **Check Response:**
   - Status Code: `500` or `404`
   - Headers: Look for `X-Correlation-ID`
   - Body: Check `ApiResponse` format

---

## 📝 Expected Results Summary

| Test Scenario | HTTP Status | Status Text | Correlation ID | Stack Trace (Prod) |
|--------------|-------------|------------|----------------|-------------------|
| Invalid endpoint | 404/500 | Exception | ✅ Yes | ❌ No |
| Validation error | 400 | ValidationError | ✅ Yes | ❌ No |
| Not found | 404 | NotFound | ✅ Yes | ❌ No |
| Unauthorized | 401 | Unauthorized | ✅ Yes | ❌ No |
| Business rule violation | 409 | BusinessRuleViolation | ✅ Yes | ❌ No |
| Timeout | 504 | Timeout | ✅ Yes | ❌ No |
| Circuit breaker | 503 | ServiceUnavailable | ✅ Yes | ❌ No |

---

## 🚨 Troubleshooting

### Issue: Correlation ID not in response
**Check:**
- Is the application running?
- Is `GlobalExceptionHandlerMiddleware` registered?
- Check middleware order in `Program.cs`

### Issue: Wrong HTTP status code
**Check:**
- Exception type mapping in `GlobalExceptionHandlerMiddleware`
- Exception being thrown matches expected type

### Issue: Stack trace exposed in production
**Check:**
- `_environment.IsDevelopment()` check
- Environment variable `ASPNETCORE_ENVIRONMENT` is not "Development"

### Issue: ApiResponse format inconsistent
**Check:**
- All exceptions go through `GlobalExceptionHandlerMiddleware`
- No controllers returning raw exceptions

---

## ✅ Success Criteria

All tests pass if:
1. ✅ Correlation IDs are included in all error responses
2. ✅ Exceptions are mapped to correct HTTP status codes
3. ✅ All error responses follow ApiResponse format
4. ✅ Internal details are NOT exposed in production
5. ✅ User-friendly error messages are provided

---

**For detailed implementation, see:** `EXCEPTION_HANDLING_COMPLETE.md`

