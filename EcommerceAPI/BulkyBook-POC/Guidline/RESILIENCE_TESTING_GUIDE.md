# Resilience Enhancements - Testing Guide

## 🧪 Testing Overview

This guide helps you test the resilience enhancements for EmailService and OAuthService, including:
- ✅ Retry logic with exponential backoff
- ✅ Circuit breaker functionality
- ✅ Error handling with specific exceptions
- ✅ Input validation
- ✅ Timeout handling

## 📋 Prerequisites

1. **Application Running:**
   ```bash
   dotnet run --project Ecommerce.API/Ecommerce.API.csproj
   ```

2. **Check Logs:**
   - Watch console output for retry and circuit breaker logs
   - Or check Application Insights/Serilog logs

3. **Test Tools:**
   - Postman, curl, or Swagger UI
   - Or use the provided PowerShell scripts

---

## 🧪 Test 1: EmailService - Retry Logic

### Objective:
Verify that EmailService retries failed SendGrid API calls with exponential backoff.

### Test Scenario 1.1: Normal Email Send (Should Succeed)
```http
POST /api/orders
Authorization: Bearer <USER_TOKEN>
Content-Type: application/json

{
  "items": [...],
  "shippingAddress": "123 Test St"
}
```

**Expected Behavior:**
- ✅ Email sent successfully
- ✅ Log: "Order confirmation email sent successfully..."
- ✅ No retries needed

### Test Scenario 1.2: Simulate Transient Failure (Retry Logic)

**Note:** This requires temporarily breaking SendGrid connection or using a test endpoint.

**What to Look For:**
- Check logs for retry attempts
- Should see: "Retry 1 after 2s", "Retry 2 after 4s", "Retry 3 after 8s"
- Exponential backoff timing (2s, 4s, 8s)

**Log Example:**
```
[Warning] Retry 1 after 2s. Status: ServiceUnavailable
[Warning] Retry 2 after 4s. Status: ServiceUnavailable
[Warning] Retry 3 after 8s. Status: ServiceUnavailable
```

### Test Scenario 1.3: Input Validation

**Test with Invalid Email:**
```http
POST /api/orders
Authorization: Bearer <USER_TOKEN>
Content-Type: application/json

{
  "items": [...],
  "shippingAddress": "123 Test St",
  "userEmail": ""  // Empty email
}
```

**Expected:**
- ❌ Should throw `ArgumentException`: "User email cannot be null or empty"
- ❌ Should NOT retry (business logic error)

---

## 🧪 Test 2: EmailService - Circuit Breaker

### Objective:
Verify that circuit breaker opens after 5 consecutive failures and prevents further calls.

### Test Scenario 2.1: Trigger Circuit Breaker

**Steps:**
1. Make 5+ consecutive requests that will fail (e.g., with invalid SendGrid API key)
2. Watch logs for circuit breaker state changes

**Expected Behavior:**
- First 5 failures: Retries occur normally
- After 5th failure: Circuit breaker opens
- Log: "Circuit breaker opened for 30s. Status: Unauthorized"
- Subsequent requests: Fail immediately with `BrokenCircuitException`
- Log: "Circuit breaker is open - SendGrid service is unavailable"

**Log Example:**
```
[Warning] Retry 1 after 2s. Status: Unauthorized
[Warning] Retry 2 after 4s. Status: Unauthorized
[Warning] Retry 3 after 8s. Status: Unauthorized
[Warning] Circuit breaker opened for 30s. Status: Unauthorized
[Error] Circuit breaker is open - SendGrid service is unavailable
```

### Test Scenario 2.2: Circuit Breaker Recovery

**Steps:**
1. Wait 30 seconds after circuit breaker opens
2. Make a new request

**Expected Behavior:**
- Log: "Circuit breaker half-open - testing if service recovered"
- If service recovered: Circuit breaker closes
- Log: "Circuit breaker reset"
- If service still failing: Circuit breaker opens again

---

## 🧪 Test 3: OAuthService - Retry Logic

### Objective:
Verify OAuthService retries failed token exchange requests.

### Test Scenario 3.1: Normal OAuth Flow (Should Succeed)

**Note:** This requires a valid OAuth provider setup.

```http
POST /api/oauth/exchange
Content-Type: application/json

{
  "code": "<VALID_AUTH_CODE>",
  "redirectUri": "https://yourapp.com/callback"
}
```

**Expected:**
- ✅ Token exchange succeeds
- ✅ Returns `OAuthTokenResult` with access token
- ✅ No retries needed

### Test Scenario 3.2: Invalid Authorization Code (Should Throw Specific Exception)

```http
POST /api/oauth/exchange
Content-Type: application/json

{
  "code": "invalid_code_12345",
  "redirectUri": "https://yourapp.com/callback"
}
```

**Expected:**
- ❌ Should throw `InvalidGrantException`
- ❌ Message: "OAuth token exchange failed: BadRequest. The authorization code may be invalid or expired."
- ❌ Should NOT retry (client error, not transient)

**Log Example:**
```
[Error] Failed to exchange code for token. Status: BadRequest, Response: {"error":"invalid_grant"}
[Error] InvalidGrantException: OAuth token exchange failed: BadRequest...
```

### Test Scenario 3.3: Transient OAuth Failure (Should Retry)

**Simulate:** Temporarily break OAuth endpoint or use invalid endpoint.

**Expected:**
- Retries with exponential backoff
- Logs retry attempts
- After 3 retries, throws `TokenExchangeException`

---

## 🧪 Test 4: OAuthService - Circuit Breaker

### Objective:
Verify circuit breaker for OAuth service.

### Test Scenario 4.1: Trigger Circuit Breaker

**Steps:**
1. Make 5+ consecutive OAuth requests that fail (e.g., wrong authority URL)
2. Watch for circuit breaker opening

**Expected:**
- After 5 failures: Circuit breaker opens
- Log: "Circuit breaker opened for 30s. Status: ServiceUnavailable"
- Subsequent requests: Fail immediately with `OAuthException`
- Message: "OAuth service is temporarily unavailable. Please try again later."

### Test Scenario 4.2: Circuit Breaker Recovery

Same as EmailService - wait 30 seconds and test recovery.

---

## 🧪 Test 5: OAuthService - Input Validation

### Objective:
Verify input validation and sanitization.

### Test Scenario 5.1: Empty Authorization Code

```http
POST /api/oauth/exchange
Content-Type: application/json

{
  "code": "",
  "redirectUri": "https://yourapp.com/callback"
}
```

**Expected:**
- ❌ Should throw `ArgumentException`: "Authorization code cannot be null or empty."
- ❌ Should NOT make HTTP request

### Test Scenario 5.2: Invalid Redirect URI Format

```http
POST /api/oauth/exchange
Content-Type: application/json

{
  "code": "valid_code",
  "redirectUri": "not-a-valid-uri"
}
```

**Expected:**
- ❌ Should throw `ArgumentException`: "Invalid redirect URI format."
- ❌ Should NOT make HTTP request

### Test Scenario 5.3: Input Sanitization

**Test with potentially dangerous characters:**
```http
POST /api/oauth/exchange
Content-Type: application/json

{
  "code": "code<script>alert('xss')</script>123",
  "redirectUri": "https://yourapp.com/callback"
}
```

**Expected:**
- ✅ Dangerous characters removed
- ✅ Only valid OAuth characters preserved
- ✅ Request proceeds with sanitized input

---

## 🧪 Test 6: Timeout Handling

### Objective:
Verify timeout handling for external calls.

### Test Scenario 6.1: OAuth Timeout

**Simulate:** Use an endpoint that takes longer than 30 seconds to respond.

**Expected:**
- ❌ Should throw `TokenExchangeException`
- ❌ Message: "OAuth token exchange timed out. Please try again."
- ❌ Log: "OAuth token exchange timed out"

**Note:** This is hard to test without a slow endpoint. You can temporarily reduce timeout in code for testing.

---

## 📊 Monitoring and Verification

### What to Check in Logs:

1. **Retry Logs:**
   ```
   [Warning] Retry {RetryCount} after {Delay}s. Status: {Status}
   ```

2. **Circuit Breaker Logs:**
   ```
   [Warning] Circuit breaker opened for {Duration}s. Status: {Status}
   [Information] Circuit breaker reset - service is healthy again
   [Information] Circuit breaker half-open - testing if service recovered
   ```

3. **Error Logs:**
   ```
   [Error] Failed to exchange code for token. Status: {Status}, Response: {Response}
   [Error] Circuit breaker is open - OAuth service is unavailable
   ```

### Application Insights / Serilog:

If using Application Insights, check:
- Retry count metrics
- Circuit breaker state changes
- Exception types and frequencies
- Request duration (should see retry delays)

---

## 🛠️ Manual Testing Scripts

### PowerShell Script: Test Email Service

```powershell
# Test Email Service Resilience
$baseUrl = "https://localhost:7273"
$token = "<YOUR_USER_TOKEN>"

# Test 1: Normal email send
Write-Host "Test 1: Normal Email Send" -ForegroundColor Green
$orderBody = @{
    items = @(@{ productId = 1; quantity = 1 })
    shippingAddress = "123 Test St"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/orders" `
        -Method POST `
        -Headers @{ Authorization = "Bearer $token" } `
        -Body $orderBody `
        -ContentType "application/json"
    Write-Host "✅ Email sent successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Check logs for retry attempts (manual verification)
Write-Host "`nCheck application logs for retry attempts" -ForegroundColor Yellow
```

### PowerShell Script: Test OAuth Service

```powershell
# Test OAuth Service Resilience
$baseUrl = "https://localhost:7273"

# Test 1: Invalid code (should throw InvalidGrantException)
Write-Host "Test 1: Invalid Authorization Code" -ForegroundColor Green
$oauthBody = @{
    code = "invalid_code_12345"
    redirectUri = "https://yourapp.com/callback"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/oauth/exchange" `
        -Method POST `
        -Body $oauthBody `
        -ContentType "application/json"
    Write-Host "❌ Should have thrown exception!" -ForegroundColor Red
} catch {
    Write-Host "✅ Exception thrown: $($_.Exception.Message)" -ForegroundColor Green
    # Check if it's InvalidGrantException
    if ($_.Exception.Message -like "*InvalidGrantException*" -or $_.Exception.Message -like "*invalid or expired*") {
        Write-Host "✅ Correct exception type!" -ForegroundColor Green
    }
}

# Test 2: Empty code (should throw ArgumentException)
Write-Host "`nTest 2: Empty Authorization Code" -ForegroundColor Green
$oauthBody = @{
    code = ""
    redirectUri = "https://yourapp.com/callback"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/oauth/exchange" `
        -Method POST `
        -Body $oauthBody `
        -ContentType "application/json"
    Write-Host "❌ Should have thrown exception!" -ForegroundColor Red
} catch {
    Write-Host "✅ Exception thrown: $($_.Exception.Message)" -ForegroundColor Green
    if ($_.Exception.Message -like "*cannot be null or empty*") {
        Write-Host "✅ Correct validation error!" -ForegroundColor Green
    }
}
```

---

## 🎯 Quick Test Checklist

### EmailService:
- [ ] Normal email send works
- [ ] Retry logs appear on transient failures
- [ ] Circuit breaker opens after 5 failures
- [ ] Circuit breaker recovers after 30 seconds
- [ ] Input validation throws ArgumentException

### OAuthService:
- [ ] Normal OAuth flow works
- [ ] InvalidGrantException thrown for invalid codes
- [ ] Retry logs appear on transient failures
- [ ] Circuit breaker opens after 5 failures
- [ ] Input validation works (empty code, invalid URI)
- [ ] Input sanitization removes dangerous characters
- [ ] Timeout handling works (if testable)

---

## 🔍 Advanced Testing

### Simulate Transient Failures:

1. **Using Fiddler/Charles Proxy:**
   - Intercept requests
   - Return 500/503 errors
   - Verify retry behavior

2. **Using Mock Server:**
   - Set up a mock OAuth/SendGrid endpoint
   - Configure to fail intermittently
   - Test retry and circuit breaker

3. **Temporary Code Changes:**
   - Temporarily reduce timeout to 1 second
   - Test timeout handling
   - Revert changes after testing

---

## 📝 Expected Results Summary

| Test Scenario | Expected Result | Exception Type |
|--------------|------------------|----------------|
| Normal email send | ✅ Success | None |
| Transient email failure | ✅ Retries 3 times | None (logs warning) |
| 5+ email failures | ✅ Circuit breaker opens | `BrokenCircuitException` |
| Invalid email input | ❌ Validation error | `ArgumentException` |
| Normal OAuth flow | ✅ Success | None |
| Invalid OAuth code | ❌ Specific error | `InvalidGrantException` |
| Transient OAuth failure | ✅ Retries 3 times | `TokenExchangeException` |
| 5+ OAuth failures | ✅ Circuit breaker opens | `OAuthException` |
| Empty OAuth code | ❌ Validation error | `ArgumentException` |
| OAuth timeout | ❌ Timeout error | `TokenExchangeException` |

---

## 🚨 Troubleshooting

### Issue: No retry logs appearing
**Solution:** Check if failures are transient (5xx) vs client errors (4xx). Only transient errors trigger retries.

### Issue: Circuit breaker not opening
**Solution:** Ensure 5 consecutive failures occur. Check if failures are being caught by retry policy first.

### Issue: Exceptions not being thrown
**Solution:** Check if error handling in controllers is catching exceptions. Verify exception types match.

---

**Need Help?** Check the application logs for detailed information about retry attempts, circuit breaker state, and exception details.

