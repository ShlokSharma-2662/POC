[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$originalEnvironment = $env:ASPNETCORE_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = 'Development'

$services = @(
    [ordered] @{
        Name = 'Ecommerce.API'
        Executable = Join-Path $repositoryRoot 'EcommerceAPI\BulkyBook-POC\Ecommerce.API\bin\Debug\net9.0\Ecommerce.API.exe'
        Url = 'http://127.0.0.1:5085'
        Probe = 'http://127.0.0.1:5085/swagger/index.html'
    },
    [ordered] @{
        Name = 'Ecommerce.OrderService'
        Executable = Join-Path $repositoryRoot 'EcommerceAPI\BulkyBook-POC\Ecommerce.OrderService\bin\Debug\net9.0\Ecommerce.OrderService.exe'
        Url = 'http://127.0.0.1:64387'
        Probe = 'http://127.0.0.1:64387/swagger/index.html'
    },
    [ordered] @{
        Name = 'Ecommerce.ProductService'
        Executable = Join-Path $repositoryRoot 'EcommerceAPI\BulkyBook-POC\Ecommerce.ProductService\bin\Debug\net9.0\Ecommerce.ProductService.exe'
        Url = 'http://127.0.0.1:64386'
        Probe = 'http://127.0.0.1:64386/swagger/index.html'
    }
)

foreach ($service in $services) {
    if (-not (Test-Path -LiteralPath $service.Executable -PathType Leaf)) {
        throw "Build output is missing for $($service.Name). Run dotnet build first."
    }
}

try {
    foreach ($service in $services) {
        $service['StandardOutput'] = [System.IO.Path]::GetTempFileName()
        $service['StandardError'] = [System.IO.Path]::GetTempFileName()
        $service['Process'] = Start-Process `
            -FilePath $service.Executable `
            -ArgumentList "--urls=$($service.Url)" `
            -WorkingDirectory (Split-Path -Parent $service.Executable) `
            -RedirectStandardOutput $service.StandardOutput `
            -RedirectStandardError $service.StandardError `
            -WindowStyle Hidden `
            -PassThru

        $ready = $false
        for ($attempt = 0; $attempt -lt 45; $attempt++) {
            if ($service.Process.HasExited) {
                break
            }

            try {
                $response = Invoke-WebRequest `
                    -UseBasicParsing `
                    -Uri $service.Probe `
                    -TimeoutSec 2
                if ($response.StatusCode -eq 200) {
                    $ready = $true
                    break
                }
            }
            catch {
                # The service may still be applying migrations or binding its port.
            }

            Start-Sleep -Seconds 1
        }

        if (-not $ready) {
            if ($service.Process.HasExited) {
                throw "$($service.Name) exited with code $($service.Process.ExitCode)."
            }

            throw "$($service.Name) did not become ready within 45 seconds."
        }

        Write-Host "$($service.Name): local Swagger returned HTTP 200."
    }
}
finally {
    foreach ($service in $services) {
        if ($service.Process -and -not $service.Process.HasExited) {
            Stop-Process -Id $service.Process.Id -Force -ErrorAction SilentlyContinue
            $service.Process.WaitForExit()
        }

        foreach ($path in @($service.StandardOutput, $service.StandardError)) {
            if ($path -and (Test-Path -LiteralPath $path -PathType Leaf)) {
                Remove-Item -LiteralPath $path -Force
            }
        }
    }

    if ($null -eq $originalEnvironment) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_ENVIRONMENT = $originalEnvironment
    }
}
