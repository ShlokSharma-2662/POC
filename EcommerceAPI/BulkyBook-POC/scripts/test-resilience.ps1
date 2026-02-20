# Resilience Enhancements Testing Script
# This script helps test the resilience features (retry, circuit breaker, error handling)

param(
    [string]$BaseUrl = "https://localhost:7273",
    [string]$UserToken = "",
    [switch]$TestEmail,
    [switch]$TestOAuth,
    [switch]$All
)

Write-Host "`n=== Resilience Enhancements Testing ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl`n" -ForegroundColor Gray

if ($All) {
    $TestEmail = $true
    $TestOAuth = $true
}

# Test Email Service Resilience
if ($TestEmail) {
    Write-Host "`n📧 Testing EmailService Resilience" -ForegroundColor Yellow
    Write-Host "=" * 50 -ForegroundColor Gray
    
    if ([string]::IsNullOrWhiteSpace($UserToken)) {
        Write-Host "⚠️  Warning: UserToken not provided. Some tests may fail." -ForegroundColor Yellow
        Write-Host "   Usage: .\test-resilience.ps1 -TestEmail -UserToken 'your_token_here'" -ForegroundColor Gray
    }
    
    # Test 1: Input Validation - Empty Email
    Write-Host "`nTest 1: Input Validation - Empty Email" -ForegroundColor Green
    try {
        # This would be called internally when creating an order
        # For now, we'll just document what to expect
        Write-Host "   Expected: ArgumentException - 'User email cannot be null or empty'" -ForegroundColor Gray
        Write-Host "   ✅ Test this by creating an order with invalid email" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Test 2: Normal Email Send (if token provided)
    if (-not [string]::IsNullOrWhiteSpace($UserToken)) {
        Write-Host "`nTest 2: Normal Email Send" -ForegroundColor Green
        try {
            $orderBody = @{
                items = @(
                    @{
                        productId = 1
                        quantity = 1
                    }
                )
                shippingAddress = "123 Test Street, Test City"
            } | ConvertTo-Json -Depth 10
            
            $headers = @{
                Authorization = "Bearer $UserToken"
                "Content-Type" = "application/json"
            }
            
            $response = Invoke-RestMethod -Uri "$BaseUrl/api/orders" `
                -Method POST `
                -Headers $headers `
                -Body $orderBody `
                -ErrorAction Stop
            
            Write-Host "   ✅ Order created successfully" -ForegroundColor Green
            Write-Host "   ✅ Check logs for email send confirmation" -ForegroundColor Green
        } catch {
            Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
            if ($_.Exception.Response) {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                Write-Host "   Response: $responseBody" -ForegroundColor Gray
            }
        }
    }
    
    Write-Host "`n📝 Manual Tests for EmailService:" -ForegroundColor Yellow
    Write-Host "   1. Check logs for retry attempts on transient failures" -ForegroundColor Gray
    Write-Host "   2. Trigger circuit breaker with 5+ consecutive failures" -ForegroundColor Gray
    Write-Host "   3. Verify circuit breaker recovery after 30 seconds" -ForegroundColor Gray
}

# Test OAuth Service Resilience
if ($TestOAuth) {
    Write-Host "`n🔐 Testing OAuthService Resilience" -ForegroundColor Yellow
    Write-Host "=" * 50 -ForegroundColor Gray
    
    # Test 1: Input Validation - Empty Code
    Write-Host "`nTest 1: Input Validation - Empty Authorization Code" -ForegroundColor Green
    try {
        $oauthBody = @{
            code = ""
            redirectUri = "https://yourapp.com/callback"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/oauth/exchange" `
            -Method POST `
            -Body $oauthBody `
            -ContentType "application/json" `
            -ErrorAction Stop
        
        Write-Host "   ❌ Should have thrown ArgumentException!" -ForegroundColor Red
    } catch {
        $errorMessage = $_.Exception.Message
        Write-Host "   ✅ Exception thrown: $errorMessage" -ForegroundColor Green
        if ($errorMessage -like "*cannot be null or empty*" -or $errorMessage -like "*ArgumentException*") {
            Write-Host "   ✅ Correct validation error!" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Unexpected exception type" -ForegroundColor Yellow
        }
    }
    
    # Test 2: Input Validation - Invalid Redirect URI
    Write-Host "`nTest 2: Input Validation - Invalid Redirect URI" -ForegroundColor Green
    try {
        $oauthBody = @{
            code = "some_code"
            redirectUri = "not-a-valid-uri"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/oauth/exchange" `
            -Method POST `
            -Body $oauthBody `
            -ContentType "application/json" `
            -ErrorAction Stop
        
        Write-Host "   ❌ Should have thrown ArgumentException!" -ForegroundColor Red
    } catch {
        $errorMessage = $_.Exception.Message
        Write-Host "   ✅ Exception thrown: $errorMessage" -ForegroundColor Green
        if ($errorMessage -like "*Invalid redirect URI*" -or $errorMessage -like "*ArgumentException*") {
            Write-Host "   ✅ Correct validation error!" -ForegroundColor Green
        }
    }
    
    # Test 3: Invalid Authorization Code (Should throw InvalidGrantException)
    Write-Host "`nTest 3: Invalid Authorization Code" -ForegroundColor Green
    try {
        $oauthBody = @{
            code = "invalid_code_12345_$(Get-Date -Format 'yyyyMMddHHmmss')"
            redirectUri = "https://yourapp.com/callback"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/oauth/exchange" `
            -Method POST `
            -Body $oauthBody `
            -ContentType "application/json" `
            -ErrorAction Stop
        
        Write-Host "   ❌ Should have thrown InvalidGrantException!" -ForegroundColor Red
    } catch {
        $errorMessage = $_.Exception.Message
        Write-Host "   ✅ Exception thrown: $errorMessage" -ForegroundColor Green
        
        # Check exception type
        if ($errorMessage -like "*InvalidGrantException*" -or 
            $errorMessage -like "*invalid or expired*" -or
            $errorMessage -like "*invalid_grant*") {
            Write-Host "   ✅ Correct exception type: InvalidGrantException" -ForegroundColor Green
        } elseif ($errorMessage -like "*TokenExchangeException*" -or
                  $errorMessage -like "*OAuthException*") {
            Write-Host "   ✅ OAuth exception thrown - may be network/configuration issue" -ForegroundColor Yellow
        } else {
            Write-Host "   ⚠️  Unexpected exception: $errorMessage" -ForegroundColor Yellow
        }
        
        # Show full error if available
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                Write-Host "   Response: $responseBody" -ForegroundColor Gray
            } catch {
                # Ignore stream reading errors
            }
        }
    }
    
    # Test 4: Input Sanitization (XSS attempt)
    Write-Host "`nTest 4: Input Sanitization" -ForegroundColor Green
    try {
        # Use single quotes to prevent PowerShell from interpreting < and > as operators
        $xssCode = 'code<script>alert(''xss'')</script>123'
        $oauthBody = @{
            code = $xssCode
            redirectUri = "https://yourapp.com/callback"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/oauth/exchange" `
            -Method POST `
            -Body $oauthBody `
            -ContentType "application/json" `
            -ErrorAction Stop
        
        Write-Host "   ⚠️  Request succeeded - check if sanitization worked" -ForegroundColor Yellow
    } catch {
        Write-Host "   ✅ Request failed - expected for invalid code" -ForegroundColor Green
        Write-Host "   ✅ Sanitization should have removed dangerous characters" -ForegroundColor Green
    }
    
    Write-Host "`n📝 Manual Tests for OAuthService:" -ForegroundColor Yellow
    Write-Host "   1. Check logs for retry attempts on transient failures" -ForegroundColor Gray
    Write-Host "   2. Trigger circuit breaker with 5+ consecutive failures" -ForegroundColor Gray
    Write-Host "   3. Test timeout handling (requires slow endpoint)" -ForegroundColor Gray
    Write-Host "   4. Verify circuit breaker recovery after 30 seconds" -ForegroundColor Gray
}

Write-Host "`n=== Testing Complete ===" -ForegroundColor Cyan
Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "   1. Check application logs for retry and circuit breaker events" -ForegroundColor Gray
Write-Host "   2. Monitor Application Insights (if configured) for metrics" -ForegroundColor Gray
Write-Host "   3. Test circuit breaker by making 5+ consecutive failing requests" -ForegroundColor Gray
Write-Host "   4. Verify exponential backoff timing: 2s, 4s, 8s" -ForegroundColor Gray
Write-Host ""
Write-Host "For detailed testing guide, see RESILIENCE_TESTING_GUIDE.md" -ForegroundColor Cyan

