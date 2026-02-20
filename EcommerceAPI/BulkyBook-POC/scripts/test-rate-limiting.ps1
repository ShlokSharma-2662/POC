# Rate Limiting Testing Script
# Validates rate limiting headers and 429 responses
# Uses Invoke-RestMethod with workaround for header issues

param(
    [string]$BaseUrl = "https://localhost:7273",
    [string]$Endpoint = "/api/products?pageNumber=1&pageSize=1",
    [string]$Method = "GET",
    [string]$Body = "",
    [hashtable]$RequestHeaders,
    [int]$Limit = 10,
    [switch]$Verbose
)

Write-Host ""
Write-Host "=== Rate Limiting Smoke Test ===" -ForegroundColor Cyan
Write-Host "Base URL : $BaseUrl" -ForegroundColor Gray
Write-Host "Endpoint : $Endpoint" -ForegroundColor Gray
Write-Host "Method   : $Method" -ForegroundColor Gray
Write-Host "Limit    : $Limit requests per window" -ForegroundColor Gray
Write-Host ""

function Show-Headers {
    param([System.Collections.Specialized.NameValueCollection]$Headers)

    $limitValue = if ($Headers['X-RateLimit-Limit']) { $Headers['X-RateLimit-Limit'] } else { '(missing)' }
    $remainingValue = if ($Headers['X-RateLimit-Remaining']) { $Headers['X-RateLimit-Remaining'] } else { '(missing)' }
    $resetValue = if ($Headers['X-RateLimit-Reset']) { $Headers['X-RateLimit-Reset'] } else { '(missing)' }

    Write-Host "   RateLimit-Limit  : $limitValue" -ForegroundColor Gray
    Write-Host "   RateLimit-Remain : $remainingValue" -ForegroundColor Gray
    Write-Host "   RateLimit-Reset  : $resetValue" -ForegroundColor Gray
    # Note: Retry-After is shown separately for 429 responses
}

# Store base headers as simple dictionary
$baseHeadersDict = @{}
if ($RequestHeaders) {
    foreach ($key in $RequestHeaders.Keys) {
        $baseHeadersDict[$key] = [string]$RequestHeaders[$key]
    }
}

$uri = "$BaseUrl$Endpoint"

# Disable SSL certificate validation for localhost (PowerShell 5.1+)
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

for ($i = 1; $i -le $Limit + 1; $i++) {
    Write-Host "Request #$i" -ForegroundColor Yellow
    
    try {
        # Build headers hashtable fresh each time - only if we have headers to add
        $headersToUse = $null
        if ($baseHeadersDict.Count -gt 0) {
            $headersToUse = @{}
            foreach ($key in $baseHeadersDict.Keys) {
                $headersToUse[$key] = $baseHeadersDict[$key]
            }
        }

        # Use WebRequest directly to have better control
        $request = [System.Net.WebRequest]::Create($uri)
        $request.Method = $Method
        
        # Add custom headers
        if ($headersToUse) {
            foreach ($key in $headersToUse.Keys) {
                if ($key -eq 'Content-Type') {
                    $request.ContentType = $headersToUse[$key]
                }
                else {
                    $request.Headers.Add($key, $headersToUse[$key])
                }
            }
        }
        
        # Add body if provided
        if (-not [string]::IsNullOrWhiteSpace($Body)) {
            if (-not $request.ContentType) {
                $request.ContentType = 'application/json'
            }
            $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
            $request.ContentLength = $bodyBytes.Length
            $requestStream = $request.GetRequestStream()
            $requestStream.Write($bodyBytes, 0, $bodyBytes.Length)
            $requestStream.Close()
        }
        
        # Get response
        $response = $request.GetResponse()
        $statusCode = [int]$response.StatusCode
        
        Write-Host "   Status           : $statusCode" -ForegroundColor Green
        Show-Headers -Headers $response.Headers
        
        if ($Verbose) {
            $responseStream = $response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($responseStream)
            $content = $reader.ReadToEnd()
            $reader.Close()
            $responseStream.Close()
            try {
                $json = $content | ConvertFrom-Json | ConvertTo-Json -Depth 4
                Write-Host "   Body (json)      : $json" -ForegroundColor DarkGray
            }
            catch {
                Write-Host "   Body (raw)       : $content" -ForegroundColor DarkGray
            }
        }
        
        $response.Close()
    }
    catch {
        $statusCode = $null
        $responseHeaders = $null
        
        if ($_.Exception.Response) {
            $httpResponse = $_.Exception.Response
            $statusCode = [int]$httpResponse.StatusCode
            $responseHeaders = $httpResponse.Headers
        }
        
        if ($statusCode) {
            $statusColor = if ($statusCode -eq 429) { "Red" } else { "Yellow" }
            Write-Host "   Status           : $statusCode" -ForegroundColor $statusColor
            if ($responseHeaders) {
                Show-Headers -Headers $responseHeaders
                
                # Show Retry-After header for 429 responses
                if ($statusCode -eq 429) {
                    $retryAfter = $responseHeaders['Retry-After']
                    if ($retryAfter) {
                        Write-Host "   Retry-After      : $retryAfter seconds" -ForegroundColor Yellow
                    }
                }
            }
            
            if ($Verbose -and $httpResponse) {
                try {
                    $stream = $httpResponse.GetResponseStream()
                    $reader = New-Object System.IO.StreamReader($stream)
                    $body = $reader.ReadToEnd()
                    $reader.Close()
                    $stream.Close()
                    Write-Host "   Body (raw)       : $body" -ForegroundColor DarkGray
                }
                catch {
                    # Ignore stream reading errors
                }
            }
        }
        else {
            # Check if it's a 429 error in the exception message
            if ($_.Exception.Message -match "429") {
                Write-Host "   Status           : 429 (Too Many Requests)" -ForegroundColor Red
                Write-Host "   [Note: Headers not available in error response]" -ForegroundColor Gray
            }
            else {
                Write-Host "   [ERROR] $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    Write-Host ""
}

Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "  - Check if rate limit headers appear on successful requests" -ForegroundColor Gray
Write-Host "  - Check if 429 status appears when limit is exceeded" -ForegroundColor Gray
Write-Host "  - Check if Retry-After header appears on 429 responses" -ForegroundColor Gray
Write-Host ""
