# Debug script to test password verification for existing users
param(
    [string]$BaseUrl = "http://localhost:5085"
)

Write-Host "🔍 Debugging Password Issues" -ForegroundColor Green

# Common test passwords to try
$testPasswords = @(
    "password",
    "Password",
    "Password123",
    "Password123!",
    "TestPassword123!",
    "admin",
    "Admin",
    "Admin123",
    "Admin123!",
    "123456",
    "12345678",
    "qwerty",
    "Qwerty123",
    "Qwerty123!"
)

# Test users from your database
$testUsers = @(
    @{Email="admin@admin.com"; Name="Admin User"},
    @{Email="test@example.com"; Name="Test User"},
    @{Email="sankalp.kamdi@rysun.com"; Name="Sankalp"},
    @{Email="suttamshlok@gmail.com"; Name="Shlok"}
)

Write-Host "`n🧪 Testing login attempts with common passwords..." -ForegroundColor Cyan

foreach ($user in $testUsers) {
    Write-Host "`n👤 Testing user: $($user.Name) ($($user.Email))" -ForegroundColor Yellow
    
    foreach ($password in $testPasswords) {
        $loginBody = @{
            Email = $user.Email
            Password = $password
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json" -ErrorAction Stop
            Write-Host "✅ SUCCESS! Password found: '$password'" -ForegroundColor Green
            Write-Host "   Token: $($response.Data.Token.Substring(0, 20))..." -ForegroundColor Gray
            break
        } catch {
            # Continue to next password
        }
    }
}

Write-Host "`n📋 Manual Test Instructions:" -ForegroundColor Cyan
Write-Host "1. Try these common passwords with your users:" -ForegroundColor White
Write-Host "   - admin@admin.com: try 'admin', 'Admin', 'Admin123', 'Admin123!'" -ForegroundColor Gray
Write-Host "   - test@example.com: try 'TestPassword123!'" -ForegroundColor Gray
Write-Host "   - sankalp.kamdi@rysun.com: try 'password', 'Password', 'Password123'" -ForegroundColor Gray
Write-Host "`n2. Check the API console output for debug information" -ForegroundColor White
Write-Host "3. Make sure there are no extra spaces in your password" -ForegroundColor White

Write-Host "`n🔧 Quick Test Commands:" -ForegroundColor Cyan
Write-Host "# Test admin user:" -ForegroundColor Gray
Write-Host '$body = @{Email="admin@admin.com"; Password="admin"} | ConvertTo-Json' -ForegroundColor DarkGray
Write-Host 'Invoke-RestMethod -Uri "http://localhost:5085/api/auth/login" -Method POST -Body $body -ContentType "application/json"' -ForegroundColor DarkGray

Write-Host "`n# Test with TestPassword123!:" -ForegroundColor Gray
Write-Host '$body = @{Email="test@example.com"; Password="TestPassword123!"} | ConvertTo-Json' -ForegroundColor DarkGray
Write-Host 'Invoke-RestMethod -Uri "http://localhost:5085/api/auth/login" -Method POST -Body $body -ContentType "application/json"' -ForegroundColor DarkGray
