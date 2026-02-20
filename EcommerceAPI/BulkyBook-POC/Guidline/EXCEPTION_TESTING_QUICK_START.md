# Exception Handling - Quick Test Guide

## 🚀 5-Minute Test

### Step 1: Start Application
```bash
cd EcommerceAPI\BulkyBook-POC
dotnet run --project Ecommerce.API\Ecommerce.API.csproj
```

### Step 2: Open Swagger
Navigate to: `https://localhost:7273/swagger`

### Step 3: Run These 3 Tests

#### Test 1: Correlation ID (30 seconds)
1. In Swagger, try: `GET /api/this-does-not-exist`
2. **Check:**
   - Response has `X-Correlation-ID` header
   - Response body has `data.correlationId`
   - ✅ **PASS** if both present

#### Test 2: Exception Mapping (1 minute)
1. Try: `POST /api/auth/register` with empty body `{}`
2. **Check:**
   - Status code is `400`
   - Response has `"status": "ValidationError"`
   - ✅ **PASS** if both correct

#### Test 3: ApiResponse Format (30 seconds)
1. Try: `GET /api/invalid-endpoint`
2. **Check Response:**
   ```json
   {
     "isSuccessful": false,
     "status": "Exception",
     "statusReason": "...",
     "data": {
       "correlationId": "...",
       "timestamp": "..."
     }
   }
   ```
   - ✅ **PASS** if all fields present

---

## ✅ All Tests Pass?

If all 3 tests pass, your exception handling is working correctly! 🎉

---

## 📝 Detailed Testing

For comprehensive testing, see:
- `EXCEPTION_HANDLING_TEST_GUIDE.md` - Full testing guide
- `EXCEPTION_HANDLING_API_TEST.md` - API-specific tests
- `scripts/test-exception-handling.ps1` - Automated test script

