# Resilience Enhancements Testing Script (Simple Version)
# This script helps test the resilience features (retry, circuit breaker, error handling)

param(
    [string]$BaseUrl = "https://localhost:7273",
    [string]$UserToken = "",
    [switch]$TestEmail,
    [switch]$TestOAuth,
    [switch]$All
)

Write-Host ""
Write-Host "=== Resilience Enhancements Testing ===" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host ""

if ($All) {
    $TestEmail = $true
    $TestOAuth = $true
}

# Test OAuth Service Resilience
if ($TestOAuth) {
    Write-Host "Testing OAuthService Resilience" -ForegroundColor Yellow
    Write-Host ("=" * 50) -ForegroundColor Gray
    Write-Host ""
    
    # Test 1: Input Validation - Empty Code
    Write-Host "Test 1: Input Validation - Empty Authorization Code" -ForegroundColor Green
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

        Write-Host "   [FAIL] Should have thrown ArgumentException!" -ForegroundColor Red
    } catch {
        $errorMessage = $_.Exception.Message
        Write-Host "   [OK] Exception thrown: $errorMessage" -ForegroundColor Green
        if ($errorMessage -like "*cannot be null or empty*" -or $errorMessage -like "*ArgumentException*") {
            Write-Host "   [OK] Correct validation error!" -ForegroundColor Green
        } else {
            Write-Host "   [WARN] Unexpected exception type" -ForegroundColor Yellow
        }
    }
    Write-Host ""
    
    # Test 2: Input Validation - Invalid Redirect URI
    Write-Host "Test 2: Input Validation - Invalid Redirect URI" -ForegroundColor Green
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

        Write-Host "   [FAIL] Should have thrown ArgumentException!" -ForegroundColor Red
    } catch {
        $errorMessage = $_.Exception.Message
        Write-Host "   [OK] Exception thrown: $errorMessage" -ForegroundColor Green
        if ($errorMessage -like "*Invalid redirect URI*" -or $errorMessage -like "*ArgumentException*") {
            Write-Host "   [OK] Correct validation error!" -ForegroundColor Green
        }
    }
    Write-Host ""
    
    # Test 3: Invalid Authorization Code
    Write-Host "Test 3: Invalid Authorization Code" -ForegroundColor Green
    try {
        $timestamp = Get-Date -Format 'yyyyMMddHHmmss'
        $oauthBody = @{
            code = "invalid_code_12345_$timestamp"
            redirectUri = "https://yourapp.com/callback"
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$BaseUrl/api/oauth/exchange" `
            -Method POST `
            -Body $oauthBody `
            -ContentType "application/json" `
            -ErrorAction Stop

        Write-Host "   [FAIL] Should have thrown InvalidGrantException!" -ForegroundColor Red
    } catch {
        $errorMessage = $_.Exception.Message
        Write-Host "   [OK] Exception thrown: $errorMessage" -ForegroundColor Green
        
        if ($errorMessage -like "*InvalidGrantException*" -or 
            $errorMessage -like "*invalid or expired*" -or
            $errorMessage -like "*invalid_grant*") {
            Write-Host "   [OK] Correct exception type: InvalidGrantException" -ForegroundColor Green
        } elseif ($errorMessage -like "*TokenExchangeException*" -or
                  $errorMessage -like "*OAuthException*") {
            Write-Host "   [OK] OAuth exception thrown - may be network/configuration issue" -ForegroundColor Yellow
        } else {
            Write-Host "   [WARN] Unexpected exception: $errorMessage" -ForegroundColor Yellow
        }
        
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
    Write-Host ""
    
    Write-Host "Manual Tests for OAuthService:" -ForegroundColor Yellow
    Write-Host "   1. Check logs for retry attempts on transient failures" -ForegroundColor Gray
    Write-Host "   2. Trigger circuit breaker with 5+ consecutive failures" -ForegroundColor Gray
    Write-Host "   3. Test timeout handling (requires slow endpoint)" -ForegroundColor Gray
    Write-Host "   4. Verify circuit breaker recovery after 30 seconds" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "=== Testing Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Check application logs for retry and circuit breaker events" -ForegroundColor Gray
Write-Host "   2. Monitor Application Insights (if configured) for metrics" -ForegroundColor Gray
Write-Host "   3. Test circuit breaker by making 5+ consecutive failing requests" -ForegroundColor Gray
Write-Host "   4. Verify exponential backoff timing: 2s, 4s, 8s" -ForegroundColor Gray
Write-Host ""
Write-Host "For detailed testing guide, see RESILIENCE_TESTING_GUIDE.md" -ForegroundColor Cyan
Write-Host ""

