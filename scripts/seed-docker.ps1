[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$database = 'EcommerceDB'
$seedScript = Join-Path $PSScriptRoot 'seed-localdb.sql'
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$sqlcmdMaster = 'export SQLCMDPASSWORD="$SA_PASSWORD"; exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d master -b -V 16'
$sqlcmdDatabase = 'export SQLCMDPASSWORD="$SA_PASSWORD"; exec /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d EcommerceDB -b -V 16'

if (-not (Get-Command 'docker.exe' -ErrorAction SilentlyContinue)) {
    throw 'Docker is not installed or docker.exe is not on PATH.'
}

if (-not (Test-Path -LiteralPath $seedScript)) {
    throw "Seed SQL file was not found: $seedScript"
}

function Invoke-ContainerSql {
    param(
        [Parameter(Mandatory)]
        [string]$Sql,

        [Parameter(Mandatory)]
        [string]$ContainerCommand
    )

    $output = $Sql |
        & docker compose exec -T sqlserver bash -lc $ContainerCommand 2>&1

    return @{
        ExitCode = $LASTEXITCODE
        Output = @($output)
    }
}

Push-Location $repositoryRoot
try {
    Write-Host 'Starting Docker infrastructure...'
    & docker compose up -d sqlserver redis rabbitmq
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to start Docker infrastructure. docker compose exited with code $LASTEXITCODE."
    }

    $serverReady = $false
    $lastProbeOutput = @()
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        $probe = Invoke-ContainerSql `
            -Sql "SET NOCOUNT ON; SELECT 1 AS Ready;`nGO" `
            -ContainerCommand $sqlcmdMaster

        if ($probe.ExitCode -eq 0) {
            $serverReady = $true
            break
        }

        $lastProbeOutput = $probe.Output
        Start-Sleep -Seconds 2
    }

    if (-not $serverReady) {
        $details = $lastProbeOutput -join [Environment]::NewLine
        throw "SQL Server did not become ready within 60 seconds.`n$details"
    }

    $schemaProbeSql = @'
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.ApplicationUsers', N'U') IS NULL
    THROW 51001, 'Application schema is not ready.', 1;
GO
'@

    $schemaProbe = Invoke-ContainerSql `
        -Sql $schemaProbeSql `
        -ContainerCommand $sqlcmdDatabase

    if ($schemaProbe.ExitCode -ne 0) {
        Write-Host 'Creating the application database through Ecommerce.API...'
        & docker compose up -d ecommerce-api
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to start Ecommerce.API. docker compose exited with code $LASTEXITCODE."
        }

        # The API initializes the schema with EnsureCreated. Restarting here
        # handles an API container that started before SQL Server was ready.
        & docker compose restart ecommerce-api
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to restart Ecommerce.API. docker compose exited with code $LASTEXITCODE."
        }

        $schemaReady = $false
        $lastProbeOutput = @()
        for ($attempt = 1; $attempt -le 45; $attempt++) {
            $schemaProbe = Invoke-ContainerSql `
                -Sql $schemaProbeSql `
                -ContainerCommand $sqlcmdDatabase

            if ($schemaProbe.ExitCode -eq 0) {
                $schemaReady = $true
                break
            }

            $lastProbeOutput = $schemaProbe.Output
            Start-Sleep -Seconds 2
        }

        if (-not $schemaReady) {
            $details = $lastProbeOutput -join [Environment]::NewLine
            throw "The EcommerceDB schema did not become ready within 90 seconds.`n$details"
        }
    }

    Write-Host "Seeding $database inside ecommerce-sqlserver..."
    $seedSql = Get-Content -Raw -LiteralPath $seedScript -Encoding UTF8
    $seedResult = Invoke-ContainerSql `
        -Sql $seedSql `
        -ContainerCommand $sqlcmdDatabase

    $seedResult.Output | ForEach-Object { Write-Host $_ }
    if ($seedResult.ExitCode -ne 0) {
        throw "Docker SQL seed failed with exit code $($seedResult.ExitCode)."
    }

    Write-Host 'Invalidating Docker catalog caches...'
    foreach ($pattern in @('products_*', 'categories_*', 'users_*')) {
        $keys = @(
            & docker compose exec -T redis redis-cli --scan --pattern $pattern
        )

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Unable to scan Redis cache keys matching $pattern."
            continue
        }

        foreach ($key in $keys) {
            if (-not [string]::IsNullOrWhiteSpace($key)) {
                & docker compose exec -T redis redis-cli UNLINK $key |
                    Out-Null
            }
        }
    }

    Write-Host ''
    Write-Host 'Docker seed completed.'
    Write-Host 'Admin: admin@local.test / Admin123!'
    Write-Host 'User:  user@local.test  / User123!'
}
finally {
    Pop-Location
}
