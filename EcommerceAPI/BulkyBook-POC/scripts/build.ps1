# Build script for Ecommerce API
param(
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Test,
    [switch]$Docker
)

Write-Host "🚀 Building Ecommerce API..." -ForegroundColor Green

# Clean solution if requested
if ($Clean) {
    Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
    dotnet clean
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue bin, obj
}

# Restore packages
Write-Host "📦 Restoring packages..." -ForegroundColor Yellow
dotnet restore

# Build solution
Write-Host "🔨 Building solution in $Configuration mode..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Run tests if requested
if ($Test) {
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build --verbosity normal
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed!" -ForegroundColor Red
        exit 1
    }
}

# Build Docker image if requested
if ($Docker) {
    Write-Host "🐳 Building Docker image..." -ForegroundColor Yellow
    docker build -t ecommerce-api:latest .
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Docker build failed!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ Build completed successfully!" -ForegroundColor Green
