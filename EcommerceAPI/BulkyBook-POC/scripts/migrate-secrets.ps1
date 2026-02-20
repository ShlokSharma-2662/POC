# PowerShell script to migrate secrets from appsettings.json to User Secrets
# Run this script from the Ecommerce.API project directory

Write-Host "🔐 Migrating Secrets to User Secrets" -ForegroundColor Cyan
Write-Host ""

# Check if we're in the right directory
if (-not (Test-Path "Ecommerce.API.csproj")) {
    Write-Host "❌ Error: Please run this script from the Ecommerce.API project directory" -ForegroundColor Red
    Write-Host "   Expected location: EcommerceAPI\BulkyBook-POC\Ecommerce.API\" -ForegroundColor Yellow
    exit 1
}

# Initialize User Secrets if not already done
Write-Host "Initializing User Secrets..." -ForegroundColor Yellow
dotnet user-secrets init

Write-Host ""
Write-Host "Setting up secrets from your current appsettings.json..." -ForegroundColor Yellow
Write-Host ""

# Read current appsettings.json to extract secrets
$appsettingsPath = "appsettings.json"
if (-not (Test-Path $appsettingsPath)) {
    Write-Host "❌ Error: appsettings.json not found" -ForegroundColor Red
    exit 1
}

$appsettings = Get-Content $appsettingsPath | ConvertFrom-Json

# JWT Secret Key
if ($appsettings.Jwt.SecretKey) {
    $jwtSecret = $appsettings.Jwt.SecretKey
    Write-Host "Setting JWT Secret Key..." -ForegroundColor Green
    dotnet user-secrets set "Jwt:SecretKey" $jwtSecret
    Write-Host "✅ JWT Secret Key set" -ForegroundColor Green
}

# OAuth Client Secret
if ($appsettings.OAuth.ClientSecret) {
    $oauthSecret = $appsettings.OAuth.ClientSecret
    Write-Host "Setting OAuth Client Secret..." -ForegroundColor Green
    dotnet user-secrets set "OAuth:ClientSecret" $oauthSecret
    Write-Host "✅ OAuth Client Secret set" -ForegroundColor Green
}

# SendGrid API Key
if ($appsettings.SendGrid.ApiKey) {
    $sendGridKey = $appsettings.SendGrid.ApiKey
    Write-Host "Setting SendGrid API Key..." -ForegroundColor Green
    dotnet user-secrets set "SendGrid:ApiKey" $sendGridKey
    Write-Host "✅ SendGrid API Key set" -ForegroundColor Green
}

# Stripe Secret Key
if ($appsettings.Stripe.SecretKey) {
    $stripeKey = $appsettings.Stripe.SecretKey
    Write-Host "Setting Stripe Secret Key..." -ForegroundColor Green
    dotnet user-secrets set "Stripe:SecretKey" $stripeKey
    Write-Host "✅ Stripe Secret Key set" -ForegroundColor Green
}

# Redis Password
if ($appsettings.Redis.Password -and $appsettings.Redis.Password -ne "") {
    $redisPassword = $appsettings.Redis.Password
    Write-Host "Setting Redis Password..." -ForegroundColor Green
    dotnet user-secrets set "Redis:Password" $redisPassword
    Write-Host "✅ Redis Password set" -ForegroundColor Green
}

# RabbitMQ Password
if ($appsettings.RabbitMQ.Password) {
    $rabbitMqPassword = $appsettings.RabbitMQ.Password
    Write-Host "Setting RabbitMQ Password..." -ForegroundColor Green
    dotnet user-secrets set "RabbitMQ:Password" $rabbitMqPassword
    Write-Host "✅ RabbitMQ Password set" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎉 Secrets migration complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Verify secrets: dotnet user-secrets list" -ForegroundColor Yellow
Write-Host "2. Update appsettings.json files to remove secrets (use placeholders)" -ForegroundColor Yellow
Write-Host "3. Test the application: dotnet run" -ForegroundColor Yellow
Write-Host ""
Write-Host "⚠️  IMPORTANT: After verifying everything works, remove secrets from appsettings.json files!" -ForegroundColor Red

