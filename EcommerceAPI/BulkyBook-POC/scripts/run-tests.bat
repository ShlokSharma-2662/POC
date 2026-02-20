@echo off
echo 🧪 Running Ecommerce API Tests
echo =================================

REM Change to the solution directory
cd /d "%~dp0.."

REM Create output directory if it doesn't exist
if not exist "TestResults" mkdir TestResults

REM Build the solution first
echo 🔨 Building solution...
dotnet build --configuration Release --no-restore

if %ERRORLEVEL% neq 0 (
    echo ❌ Build failed!
    exit /b 1
)

REM Run tests with coverage
echo 🚀 Executing tests with coverage...
dotnet test --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory "TestResults"

if %ERRORLEVEL% equ 0 (
    echo ✅ All tests passed!
    echo 📊 Coverage report generated in: TestResults
    echo 💡 Install reportgenerator tool for HTML coverage reports:
    echo    dotnet tool install -g dotnet-reportgenerator-globaltool
) else (
    echo ❌ Some tests failed!
    exit /b 1
)

echo 🎉 Test execution completed!
pause

