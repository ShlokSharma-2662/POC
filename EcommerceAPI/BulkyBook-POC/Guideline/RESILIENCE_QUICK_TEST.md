# Quick Resilience Testing Guide

## 🚀 Quick Start (5 Minutes)

### Step 1: Start the Application
```bash
cd EcommerceAPI/BulkyBook-POC
dotnet run --project Ecommerce.API/Ecommerce.API.csproj
```

### Step 2: Open Swagger UI
Navigate to: `https://localhost:7273/swagger`

### Step 3: Run Quick Tests

## ✅ Test 1: OAuth Input Validation (30 seconds)

### Test Empty Authorization Code:
1. In Swagger, find the OAuth endpoint (if available)
2. Or use PowerShell:
```powershell
.\scripts\test-resilience.ps1 -TestOAuth
```

**Expected Result:**
- ✅ Should return `400 Bad Request`
- ✅ Error message: "Authorization code cannot be null or empty"
- ✅ Check logs: Should NOT make HTTP request (validation failed first)

## ✅ Test 2: OAuth Invalid Code (30 seconds)

### Test Invalid Authorization Code:
```powershell
# In PowerShell
$body = @{
    code = "invalid_code_12345"
    redirectUri = "https://yourapp.com/callback"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7273/api/oauth/exchange" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

**Expected Result:**
- ❌ Should throw exception
- ✅ Check logs for retry attempts (if transient failure)
- ✅ Check logs for specific exception type (`InvalidGrantException` or `TokenExchangeException`)

## ✅ Test 3: Email Service Input Validation (30 seconds)

### Test Empty Email:
This is tested when creating an order. The EmailService will validate the email when sending confirmation.

**To Test:**
1. Create an order with invalid/empty email (if your API allows)
2. Check logs for `ArgumentException`

**Expected Result:**
- ✅ Should throw `ArgumentException`: "User email cannot be null or empty"
- ✅ Should NOT retry (business logic error)

## ✅ Test 4: Monitor Logs for Retry Behavior (2 minutes)

### What to Look For:

1. **Start the application and watch console logs**

2. **Make a request that will fail transiently** (e.g., temporarily break SendGrid connection)

3. **Look for retry logs:**
   ```
   [Warning] Retry 1 after 2s. Status: ServiceUnavailable
   [Warning] Retry 2 after 4s. Status: ServiceUnavailable
   [Warning] Retry 3 after 8s. Status: ServiceUnavailable
   ```

4. **Verify timing:**
   - First retry: ~2 seconds after initial failure
   - Second retry: ~4 seconds after first retry
   - Third retry: ~8 seconds after second retry

## ✅ Test 5: Circuit Breaker (3 minutes)

### Steps:
1. **Make 5+ consecutive failing requests** (e.g., with invalid SendGrid API key or OAuth configuration)

2. **Watch for circuit breaker opening:**
   ```
   [Warning] Circuit breaker opened for 30s. Status: Unauthorized
   ```

3. **Make another request immediately:**
   - Should fail fast (no retry)
   - Log: "Circuit breaker is open - SendGrid service is unavailable"

4. **Wait 30 seconds**

5. **Make another request:**
   - Log: "Circuit breaker half-open - testing if service recovered"
   - If service recovered: "Circuit breaker reset"
   - If still failing: Circuit breaker opens again

## 📊 Quick Verification Checklist

- [ ] Empty OAuth code throws `ArgumentException`
- [ ] Invalid OAuth code throws specific exception (`InvalidGrantException` or `TokenExchangeException`)
- [ ] Retry logs appear on transient failures
- [ ] Exponential backoff timing is correct (2s, 4s, 8s)
- [ ] Circuit breaker opens after 5 failures
- [ ] Circuit breaker prevents requests when open
- [ ] Circuit breaker recovers after 30 seconds

## 🛠️ Using the Test Script

### Run All Tests:
```powershell
.\scripts\test-resilience.ps1 -All
```

### Test Only OAuth:
```powershell
.\scripts\test-resilience.ps1 -TestOAuth
```

### Test Only Email:
```powershell
.\scripts\test-resilience.ps1 -TestEmail -UserToken "your_token_here"
```

### Custom Base URL:
```powershell
.\scripts\test-resilience.ps1 -BaseUrl "https://localhost:5001" -TestOAuth
```

## 📝 What Success Looks Like

### ✅ Successful Test Results:

1. **Input Validation:**
   - Empty/invalid inputs throw `ArgumentException` immediately
   - No HTTP requests made for invalid inputs

2. **Retry Logic:**
   - Transient failures trigger retries
   - Exponential backoff timing is correct
   - Logs show retry attempts

3. **Circuit Breaker:**
   - Opens after 5 consecutive failures
   - Prevents requests when open
   - Recovers after 30 seconds
   - Logs state changes

4. **Error Handling:**
   - Specific exceptions thrown (`InvalidGrantException`, `TokenExchangeException`, etc.)
   - Clear error messages
   - Proper exception propagation

## 🚨 Common Issues

### Issue: No retry logs appearing
**Possible Causes:**
- Failures are client errors (4xx), not transient (5xx)
- Circuit breaker already open
- Exception thrown before retry policy

**Solution:** Check exception type and HTTP status code

### Issue: Circuit breaker not opening
**Possible Causes:**
- Not enough consecutive failures (need 5)
- Failures are being caught before circuit breaker
- Different exception types (circuit breaker only tracks specific exceptions)

**Solution:** Ensure 5 consecutive failures of the same type

### Issue: Exceptions not being thrown
**Possible Causes:**
- Controller error handling catching exceptions
- Exception type mismatch

**Solution:** Check controller error handling and exception types

---

**For detailed testing scenarios, see:** `RESILIENCE_TESTING_GUIDE.md`

