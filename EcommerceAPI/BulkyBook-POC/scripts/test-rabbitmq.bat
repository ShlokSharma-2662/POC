@echo off
echo ========================================
echo RabbitMQ Implementation Test Script
echo ========================================
echo.

echo 1. Checking if RabbitMQ Docker container is running...
docker ps | findstr rabbitmq >nul
if %errorlevel% equ 0 (
    echo ✓ RabbitMQ container is running
) else (
    echo ✗ RabbitMQ container is not running
    echo.
    echo Starting RabbitMQ container...
    docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=guest -e RABBITMQ_DEFAULT_PASS=guest rabbitmq:3-management
    echo ✓ RabbitMQ container started
    echo.
    echo Waiting for RabbitMQ to initialize...
    timeout /t 10 /nobreak >nul
)

echo.
echo 2. Testing RabbitMQ connection...
curl -s -u guest:guest http://localhost:15672/api/overview >nul
if %errorlevel% equ 0 (
    echo ✓ RabbitMQ is accessible
) else (
    echo ✗ RabbitMQ is not accessible
    echo Please check if RabbitMQ is running on localhost:15672
)

echo.
echo 3. RabbitMQ Management UI Information:
echo    URL: http://localhost:15672
echo    Username: guest
echo    Password: guest
echo.

echo 4. Next Steps:
echo    - Enable RabbitMQ in appsettings.json: "RabbitMQ:Enabled": true
echo    - Start your E-Commerce API
echo    - Test order creation or user registration
echo    - Monitor messages in RabbitMQ Management UI
echo.

echo ========================================
echo Test completed!
echo ========================================
pause





