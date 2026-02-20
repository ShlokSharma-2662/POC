# PowerShell script to set up User Secrets for development
# Run this script from the Ecommerce.API project directory

Write-Host "🔐 Setting up User Secrets for E-Commerce API" -ForegroundColor Cyan
Write-Host ""

# Check if we're in the right directory
if (-not (Test-Path "Ecommerce.API.csproj")) {
    Write-Host "❌ Error: Please run this script from the Ecommerce.API project directory" -ForegroundColor Red
    exit 1
}

# Initialize User Secrets if not already done
Write-Host "Initializing User Secrets..." -ForegroundColor Yellow
dotnet user-secrets init

Write-Host ""
Write-Host "Please enter the following secrets. Press Enter to skip optional values." -ForegroundColor Yellow
Write-Host ""

# JWT Secret Key
$jwtSecret = Read-Host "JWT Secret Key (minimum 32 characters, or press Enter to generate)"
if ([string]::IsNullOrWhiteSpace($jwtSecret)) {
    # Generate a random secret key
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $jwtSecret = [Convert]::ToBase64String($bytes)
    Write-Host "Generated JWT Secret Key: $jwtSecret" -ForegroundColor Green
}
dotnet user-secrets set "Jwt:SecretKey" $jwtSecret
Write-Host "✅ JWT Secret Key set" -ForegroundColor Green

# OAuth Client Secret
$oauthSecret = Read-Host "OAuth Client Secret (or press Enter to skip)"
if (-not [string]::IsNullOrWhiteSpace($oauthSecret)) {
    dotnet user-secrets set "OAuth:ClientSecret" $oauthSecret
    Write-Host "✅ OAuth Client Secret set" -ForegroundColor Green
}

# SendGrid API Key
$sendGridKey = Read-Host "SendGrid API Key (or press Enter to skip)"
if (-not [string]::IsNullOrWhiteSpace($sendGridKey)) {
    dotnet user-secrets set "SendGrid:ApiKey" $sendGridKey
    Write-Host "✅ SendGrid API Key set" -ForegroundColor Green
}

# Stripe Secret Key
$stripeKey = Read-Host "Stripe Secret Key (or press Enter to skip)"
if (-not [string]::IsNullOrWhiteSpace($stripeKey)) {
    dotnet user-secrets set "Stripe:SecretKey" $stripeKey
    Write-Host "✅ Stripe Secret Key set" -ForegroundColor Green
}

# Redis Password
$redisPassword = Read-Host "Redis Password (or press Enter to skip)"
if (-not [string]::IsNullOrWhiteSpace($redisPassword)) {
    dotnet user-secrets set "Redis:Password" $redisPassword
    Write-Host "✅ Redis Password set" -ForegroundColor Green
}

# RabbitMQ Password
$rabbitMqPassword = Read-Host "RabbitMQ Password (or press Enter to skip)"
if (-not [string]::IsNullOrWhiteSpace($rabbitMqPassword)) {
    dotnet user-secrets set "RabbitMQ:Password" $rabbitMqPassword
    Write-Host "✅ RabbitMQ Password set" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎉 User Secrets setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To view all secrets, run: dotnet user-secrets list" -ForegroundColor Cyan
Write-Host "To remove a secret, run: dotnet user-secrets remove \"Key:Name\"" -ForegroundColor Cyan

