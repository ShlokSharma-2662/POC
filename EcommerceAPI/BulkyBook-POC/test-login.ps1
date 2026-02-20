# Test script to check login functionality
param(
    [string]$BaseUrl = "http://localhost:5085",
    [string]$Email = "test@example.com",
    [string]$Password = "TestPassword123!",
    [string]$FirstName = "Test",
    [string]$LastName = "User"
)

Write-Host "🧪 Testing Ecommerce API Login Functionality" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow

# Test 1: Register a new user
Write-Host "`n📝 Step 1: Registering a new user..." -ForegroundColor Cyan
$registerBody = @{
    Email = $Email
    Password = $Password
    FirstName = $FirstName
    LastName = $LastName
    Role = "User"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✅ Registration successful!" -ForegroundColor Green
    Write-Host "Response: $($registerResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Registration failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error details: $errorBody" -ForegroundColor Red
    }
}

# Test 2: Login with the registered user
Write-Host "`n🔐 Step 2: Logging in with registered user..." -ForegroundColor Cyan
$loginBody = @{
    Email = $Email
    Password = $Password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "Token: $($loginResponse.Data.Token)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error details: $errorBody" -ForegroundColor Red
    }
}

# Test 3: Check if API is accessible
Write-Host "`n🌐 Step 3: Testing API accessibility..." -ForegroundColor Cyan
try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/swagger/v1/swagger.json" -Method GET
    Write-Host "✅ API is accessible!" -ForegroundColor Green
} catch {
    Write-Host "❌ API is not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🏁 Test completed!" -ForegroundColor Green
