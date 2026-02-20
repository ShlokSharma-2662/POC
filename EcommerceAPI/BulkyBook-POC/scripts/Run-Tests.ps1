# PowerShell script to run tests with coverage analysis
param(
    [string]$Filter = "",
    [switch]$Coverage = $false,
    [string]$OutputFormat = "cobertura",
    [string]$OutputPath = "TestResults"
)

Write-Host "🧪 Running Ecommerce API Tests" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Change to the solution directory
$solutionPath = Split-Path -Parent $PSScriptRoot
Set-Location $solutionPath

# Create output directory if it doesn't exist
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
}

# Build the solution first
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Prepare test command
$testCommand = "dotnet test --configuration Release --no-build --verbosity normal"

if ($Filter) {
    $testCommand += " --filter `"$Filter`""
    Write-Host "🔍 Running tests with filter: $Filter" -ForegroundColor Cyan
} else {
    Write-Host "🔍 Running all tests..." -ForegroundColor Cyan
}

if ($Coverage) {
    $testCommand += " --collect:`"XPlat Code Coverage`" --results-directory `"$OutputPath`""
    Write-Host "📊 Code coverage analysis enabled" -ForegroundColor Cyan
}

# Run tests
Write-Host "🚀 Executing tests..." -ForegroundColor Yellow
Invoke-Expression $testCommand

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ All tests passed!" -ForegroundColor Green
    
    if ($Coverage) {
        Write-Host "📊 Coverage report generated in: $OutputPath" -ForegroundColor Green
        
        # Try to generate HTML coverage report if reportgenerator is available
        $reportGeneratorPath = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
        if ($reportGeneratorPath) {
            Write-Host "📈 Generating HTML coverage report..." -ForegroundColor Yellow
            $coverageFiles = Get-ChildItem -Path $OutputPath -Filter "coverage.cobertura.xml" -Recurse
            if ($coverageFiles) {
                $coverageFile = $coverageFiles[0].FullName
                $htmlOutputPath = Join-Path $OutputPath "CoverageReport"
                reportgenerator -reports:"$coverageFile" -targetdir:"$htmlOutputPath" -reporttypes:"Html"
                Write-Host "📈 HTML coverage report generated in: $htmlOutputPath" -ForegroundColor Green
            }
        } else {
            Write-Host "💡 Install reportgenerator tool for HTML coverage reports:" -ForegroundColor Yellow
            Write-Host "   dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "❌ Some tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "🎉 Test execution completed!" -ForegroundColor Green

