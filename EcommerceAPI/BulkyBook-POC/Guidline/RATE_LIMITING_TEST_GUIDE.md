# Rate Limiting - Testing Guide

## 🧪 Objective
Validate that rate limiting uses Redis for distributed counters (when available), and falls back to in-memory cache when Redis is disabled.

---

## ✅ Prerequisites

1. **Redis Instance (optional but recommended)**
   - Local Docker:
     ```bash
     docker run -d --name redis -p 6379:6379 redis:7
     ```
   - Update `appsettings.Development.json`:
     ```json
     "Redis": {
       "IsCacheEnabled": "true",
       "ConnectionString": "localhost:6379"
     }
     ```

2. **Start the API**
   ```bash
   dotnet run --project Ecommerce.API/Ecommerce.API.csproj
   ```

3. **Tools**
   - Swagger UI (`https://localhost:7273/swagger`)
   - Postman / curl / PowerShell (optional)

---

## 🔄 Test Matrix

| Scenario | Redis Enabled | Expected Behaviour |
|----------|---------------|--------------------|
| 1. Rate limit below threshold | ✅ | Response headers show remaining quota |
| 2. Rate limit exceeded | ✅ | 429 with `Retry-After` header |
| 3. Redis disabled | ❌ | Fallback to in-memory counters; behaviour unchanged |
| 4. Multi-instance (optional) | ✅ | Counters shared across instances |

---

## ✅ Scenario 1: Normal Usage

**Endpoint:** `POST /api/auth/login`

**Steps:**
1. In Swagger, locate `Auth → POST /api/auth/login`
2. Submit **up to 5** requests within 1 minute
3. Observe response headers:
   - `X-RateLimit-Limit: 5`
   - `X-RateLimit-Remaining` counts down (4, 3, 2, 1, 0)
   - `X-RateLimit-Reset` increases by ~60 seconds

**Expected:** All requests return HTTP 200 (assuming valid credentials) until the limit is hit.

---

## ❌ Scenario 2: Exceeding Limit

**Endpoint:** `POST /api/auth/login`

**Steps:**
1. Send **6th** request within the same minute window
2. Observe response:
   ```json
   HTTP/1.1 429 Too Many Requests
   X-RateLimit-Limit: 5
   X-RateLimit-Remaining: 0
   X-RateLimit-Reset: 2025-01-07T12:34:56.789Z
   Retry-After: 58
   Body: { "error": "Rate limit exceeded", "message": "Too many requests. Please try again later.", "retryAfter": 58 }
   ```

**Expected:**
- HTTP Status: 429
- `Retry-After` header populated (seconds)
- Response body matches format above

---

## 🔄 Scenario 3: Redis Disabled (Fallback)

1. Set `Redis:IsCacheEnabled` to `false`
2. Restart the API
3. Repeat scenarios 1 & 2

**Expected:**
- Behaviour identical to Redis path
- Logs show message: *"Redis connection not available. Falling back to in-memory rate limiting."*

---

## 🧪 Scenario 4: Multi-Instance (Optional)

1. Start two API instances (different ports):
   ```bash
   dotnet run --project Ecommerce.API/Ecommerce.API.csproj --urls https://localhost:7273
   dotnet run --project Ecommerce.API/Ecommerce.API.csproj --urls https://localhost:7274
   ```
2. Alternate login requests between the two instances

**Expected:**
- Combined count across both instances respects the same limit (5 per minute)
- Remaining quota shared between instances

---

## 🔍 Verifying Headers with curl

```bash
# Login request (valid body omitted for brevity)
for i in {1..6}
do
  echo "Request $i"
  curl -k -s -o /dev/null -w "Status: %{http_code}\nLimit: %{http_header_X-RateLimit-Limit}\nRemaining: %{http_header_X-RateLimit-Remaining}\nReset: %{http_header_X-RateLimit-Reset}\nRetry-After: %{http_header_Retry-After}\n\n" \
    -X POST https://localhost:7273/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"user@example.com","password":"P@ssw0rd"}'
done
```

---

## 🧾 Observing Redis Keys (Optional)

```bash
redis-cli --scan --pattern "rate_limit:*"
# Inspect specific key
redis-cli HGETALL "rate_limit:POST:/api/auth/login"
```

(Actual storage uses simple strings; `GET rate_limit:...` will show current counter.)

---

## 🧭 Troubleshooting

| Symptom | Possible Cause | Fix |
|---------|----------------|-----|
| Headers missing | Middleware order incorrect | Ensure `RateLimitingMiddleware` is registered before other middleware that could short-circuit |
| Redis not used | Redis disabled or connection failed | Check logs for connection errors, verify `Redis:IsCacheEnabled` |
| Remaining count does not decrement | Requests failing before middleware runs | Confirm endpoint returns 2xx/4xx (not redirected) |
| Retry-After negative | System clock skew | Ensure server clock is synced |

---

## ✅ Success Criteria

1. Rate limit headers present on every response
2. 429 responses include `Retry-After`
3. Limits enforce per configuration rules
4. Redis path works when enabled; fallback works when disabled

---

Happy testing! 🚀
