# Deployment script for Ecommerce API
param(
    [string]$Environment = "Production",
    [string]$AzureResourceGroup = "ecommerce-rg",
    [string]$AzureAppService = "ecommerce-api",
    [switch]$Docker,
    [switch]$Azure
)

Write-Host "🚀 Deploying Ecommerce API to $Environment..." -ForegroundColor Green

# Build the application
Write-Host "🔨 Building application..." -ForegroundColor Yellow
& "$PSScriptRoot\build.ps1" -Configuration Release -Test

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

if ($Docker) {
    Write-Host "🐳 Deploying with Docker..." -ForegroundColor Yellow
    
    # Build Docker image
    docker build -t ecommerce-api:$Environment .
    
    # Tag for registry (replace with your registry)
    docker tag ecommerce-api:$Environment your-registry/ecommerce-api:$Environment
    
    # Push to registry
    docker push your-registry/ecommerce-api:$Environment
    
    Write-Host "✅ Docker deployment completed!" -ForegroundColor Green
}
elseif ($Azure) {
    Write-Host "☁️ Deploying to Azure..." -ForegroundColor Yellow
    
    # Login to Azure (if not already logged in)
    az login
    
    # Deploy to Azure App Service
    az webapp deployment source config-zip `
        --resource-group $AzureResourceGroup `
        --name $AzureAppService `
        --src "Ecommerce.API/bin/Release/net9.0/publish.zip"
    
    Write-Host "✅ Azure deployment completed!" -ForegroundColor Green
}
else {
    Write-Host "📁 Creating deployment package..." -ForegroundColor Yellow
    
    # Publish the application
    dotnet publish Ecommerce.API --configuration Release --output ./publish
    
    # Create deployment package
    Compress-Archive -Path "./publish/*" -DestinationPath "ecommerce-api-$Environment.zip" -Force
    
    Write-Host "✅ Deployment package created: ecommerce-api-$Environment.zip" -ForegroundColor Green
}

Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
