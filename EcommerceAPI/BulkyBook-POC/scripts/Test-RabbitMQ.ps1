# RabbitMQ Implementation Test Script
# PowerShell version for better compatibility

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RabbitMQ Implementation Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is available
Write-Host "1. Checking Docker availability..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version 2>$null
    if ($dockerVersion) {
        Write-Host "✓ Docker is available: $dockerVersion" -ForegroundColor Green
    } else {
        Write-Host "✗ Docker is not available" -ForegroundColor Red
        Write-Host "Please install Docker Desktop and try again" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Docker is not available" -ForegroundColor Red
    Write-Host "Please install Docker Desktop and try again" -ForegroundColor Red
    exit 1
}

# Check if RabbitMQ container is running
Write-Host ""
Write-Host "2. Checking RabbitMQ container status..." -ForegroundColor Yellow
$rabbitmqContainer = docker ps --filter "name=rabbitmq" --format "table {{.Names}}" | Select-String "rabbitmq"

if ($rabbitmqContainer) {
    Write-Host "✓ RabbitMQ container is running" -ForegroundColor Green
} else {
    Write-Host "✗ RabbitMQ container is not running" -ForegroundColor Red
    Write-Host ""
    Write-Host "Starting RabbitMQ container..." -ForegroundColor Yellow
    
    try {
        docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=guest -e RABBITMQ_DEFAULT_PASS=guest rabbitmq:3-management
        Write-Host "✓ RabbitMQ container started successfully" -ForegroundColor Green
        Write-Host ""
        Write-Host "Waiting for RabbitMQ to initialize..." -ForegroundColor Yellow
        Start-Sleep -Seconds 15
    } catch {
        Write-Host "✗ Failed to start RabbitMQ container" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Test RabbitMQ connection
Write-Host ""
Write-Host "3. Testing RabbitMQ connection..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:15672/api/overview" -Credential (New-Object System.Management.Automation.PSCredential("guest", (ConvertTo-SecureString "guest" -AsPlainText -Force))) -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ RabbitMQ is accessible and responding" -ForegroundColor Green
    } else {
        Write-Host "✗ RabbitMQ returned status code: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ RabbitMQ is not accessible" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please check if RabbitMQ is running on localhost:15672" -ForegroundColor Yellow
}

# Display connection information
Write-Host ""
Write-Host "4. RabbitMQ Connection Information:" -ForegroundColor Cyan
Write-Host "   Management UI URL: http://localhost:15672" -ForegroundColor White
Write-Host "   Username: guest" -ForegroundColor White
Write-Host "   Password: guest" -ForegroundColor White
Write-Host "   AMQP Port: 5672" -ForegroundColor White
Write-Host "   Management Port: 15672" -ForegroundColor White

# Configuration check
Write-Host ""
Write-Host "5. Configuration Check:" -ForegroundColor Cyan
$appsettingsPath = "Ecommerce.API\appsettings.json"
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
    if ($appsettings.RabbitMQ.Enabled -eq $true) {
        Write-Host "✓ RabbitMQ is enabled in appsettings.json" -ForegroundColor Green
    } else {
        Write-Host "⚠ RabbitMQ is disabled in appsettings.json" -ForegroundColor Yellow
        Write-Host "  Set 'RabbitMQ:Enabled' to 'true' to enable messaging" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠ appsettings.json not found" -ForegroundColor Yellow
}

# Next steps
Write-Host ""
Write-Host "6. Next Steps:" -ForegroundColor Cyan
Write-Host "   - Enable RabbitMQ in appsettings.json: 'RabbitMQ:Enabled': true" -ForegroundColor White
Write-Host "   - Start your E-Commerce API" -ForegroundColor White
Write-Host "   - Test order creation or user registration" -ForegroundColor White
Write-Host "   - Monitor messages in RabbitMQ Management UI" -ForegroundColor White
Write-Host "   - Check application logs for message processing" -ForegroundColor White

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test completed!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Keep window open
Read-Host "Press Enter to continue"





