# Run script for Ecommerce API
param(
    [string]$Environment = "Development",
    [switch]$Docker,
    [switch]$DockerCompose
)

Write-Host "🚀 Starting Ecommerce API..." -ForegroundColor Green

if ($DockerCompose) {
    Write-Host "🐳 Starting with Docker Compose..." -ForegroundColor Yellow
    docker-compose up --build
}
elseif ($Docker) {
    Write-Host "🐳 Starting with Docker..." -ForegroundColor Yellow
    docker run -p 5000:80 -e ASPNETCORE_ENVIRONMENT=$Environment ecommerce-api:latest
}
else {
    Write-Host "🔧 Starting with .NET CLI..." -ForegroundColor Yellow
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    dotnet run --project Ecommerce.API
}

Write-Host "✅ Ecommerce API started!" -ForegroundColor Green
Write-Host "🌐 API URL: https://localhost:7001" -ForegroundColor Cyan
Write-Host "📚 Swagger UI: https://localhost:7001/swagger" -ForegroundColor Cyan
