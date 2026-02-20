# RabbitMQ Demo Script
# This script demonstrates how RabbitMQ works with your e-commerce system

Write-Host "🐰 RabbitMQ Demo - E-Commerce Message Flow" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Start RabbitMQ
Write-Host "1. Starting RabbitMQ..." -ForegroundColor Yellow
Write-Host "   - RabbitMQ Management UI: http://localhost:15672" -ForegroundColor White
Write-Host "   - Username: guest, Password: guest" -ForegroundColor White
Write-Host ""

# Step 2: Simulate Order Creation
Write-Host "2. Simulating Order Creation..." -ForegroundColor Yellow
Write-Host "   User places order for iPhone 15 Pro" -ForegroundColor White
Write-Host "   Order ID: ORD-12345" -ForegroundColor White
Write-Host "   Amount: $999.99" -ForegroundColor White
Write-Host ""

# Step 3: Show Message Flow
Write-Host "3. Message Flow:" -ForegroundColor Yellow
Write-Host "   📤 API sends message to RabbitMQ" -ForegroundColor Green
Write-Host "   📨 RabbitMQ stores message in queue" -ForegroundColor Green
Write-Host "   📧 Email service receives message" -ForegroundColor Green
Write-Host "   ✉️  Confirmation email sent to customer" -ForegroundColor Green
Write-Host ""

# Step 4: Show Benefits
Write-Host "4. Benefits:" -ForegroundColor Yellow
Write-Host "   ⚡ User gets response in 0.1 seconds (not 5 seconds)" -ForegroundColor Green
Write-Host "   🔄 Email sending happens in background" -ForegroundColor Green
Write-Host "   🛡️  If email fails, message can be retried" -ForegroundColor Green
Write-Host "   📈 System can handle 1000+ orders per minute" -ForegroundColor Green
Write-Host ""

# Step 5: Show RabbitMQ Management
Write-Host "5. Monitor in RabbitMQ Management UI:" -ForegroundColor Yellow
Write-Host "   - Go to http://localhost:15672" -ForegroundColor White
Write-Host "   - Login with guest/guest" -ForegroundColor White
Write-Host "   - Check 'Queues' tab to see messages" -ForegroundColor White
Write-Host "   - Check 'Exchanges' tab to see routing" -ForegroundColor White
Write-Host ""

Write-Host "🎯 Key Takeaway:" -ForegroundColor Cyan
Write-Host "RabbitMQ = Smart message delivery system that makes your app faster and more reliable!" -ForegroundColor White
Write-Host ""

Read-Host "Press Enter to continue"





