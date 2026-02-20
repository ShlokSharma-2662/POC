# Rate Limiting Test Script - Low Limit Test
# Tests rate limiting with a lower limit to trigger 429 responses

param(
    [string]$BaseUrl = "https://localhost:7273",
    [int]$Limit = 60
)

Write-Host ""
Write-Host "=== Rate Limiting Low Limit Test ===" -ForegroundColor Cyan
Write-Host "Base URL : $BaseUrl" -ForegroundColor Gray
Write-Host "Testing with $Limit requests to trigger rate limit" -ForegroundColor Gray
Write-Host ""

# Disable SSL certificate validation for localhost
if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
    Add-Type @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
        public class TrustAllCertsPolicy : ICertificatePolicy {
            public bool CheckValidationResult(
                ServicePoint srvPoint, X509Certificate certificate,
                WebRequest request, int certificateProblem) {
                return true;
            }
        }
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
}

$uri = "$BaseUrl/api/products?pageNumber=1&pageSize=1"
$rateLimitExceeded = $false
$first429Request = $null

for ($i = 1; $i -le $Limit + 5; $i++) {
    try {
        $request = [System.Net.WebRequest]::Create($uri)
        $request.Method = "GET"
        $response = $request.GetResponse()
        $statusCode = [int]$response.StatusCode
        
        $limitHeader = $response.Headers['X-RateLimit-Limit']
        $remainingHeader = $response.Headers['X-RateLimit-Remaining']
        $resetHeader = $response.Headers['X-RateLimit-Reset']
        
        if ($statusCode -eq 200) {
            Write-Host "Request #$i : Status $statusCode | Remaining: $remainingHeader / $limitHeader" -ForegroundColor Green
        }
        else {
            Write-Host "Request #$i : Status $statusCode" -ForegroundColor Yellow
        }
        
        $response.Close()
        
        # Check if we're getting close to the limit
        if ($remainingHeader -and [int]$remainingHeader -le 5) {
            Write-Host "   ⚠️  Approaching rate limit! Remaining: $remainingHeader" -ForegroundColor Yellow
        }
    }
    catch {
        $httpError = $_.Exception.Response
        if ($httpError) {
            $statusCode = [int]$httpError.StatusCode
            $retryAfter = $httpError.Headers['Retry-After']
            $limitHeader = $httpError.Headers['X-RateLimit-Limit']
            $remainingHeader = $httpError.Headers['X-RateLimit-Remaining']
            $resetHeader = $httpError.Headers['X-RateLimit-Reset']
            
            if ($statusCode -eq 429) {
                if (-not $rateLimitExceeded) {
                    $rateLimitExceeded = $true
                    $first429Request = $i
                    Write-Host ""
                    Write-Host "🎯 RATE LIMIT EXCEEDED on Request #$i" -ForegroundColor Red
                    Write-Host "   Status           : 429 (Too Many Requests)" -ForegroundColor Red
                    Write-Host "   RateLimit-Limit  : $limitHeader" -ForegroundColor Gray
                    Write-Host "   RateLimit-Remain : $remainingHeader" -ForegroundColor Gray
                    Write-Host "   RateLimit-Reset  : $resetHeader" -ForegroundColor Gray
                    Write-Host "   Retry-After      : $retryAfter seconds" -ForegroundColor Yellow
                    Write-Host ""
                }
                else {
                    Write-Host "Request #$i : Status 429 (Rate Limited) | Retry-After: $retryAfter seconds" -ForegroundColor Red
                }
            }
            else {
                Write-Host "Request #$i : Status $statusCode" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "Request #$i : [ERROR] $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # Small delay to avoid overwhelming the server
    Start-Sleep -Milliseconds 50
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
if ($rateLimitExceeded) {
    Write-Host "✅ Rate limiting is working! 429 response received on request #$first429Request" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Rate limit not exceeded. Try increasing the number of requests or wait for the window to reset." -ForegroundColor Yellow
}
Write-Host ""

