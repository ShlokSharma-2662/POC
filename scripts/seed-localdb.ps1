[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$server = '(localdb)\MSSQLLocalDB'
$database = 'ECommercePOC'
$scriptPath = Join-Path $PSScriptRoot 'seed-localdb.sql'

if (-not (Get-Command 'SqlLocalDB.exe' -ErrorAction SilentlyContinue)) {
    throw 'SQL Server LocalDB is not installed or SqlLocalDB.exe is not on PATH.'
}

if (-not (Get-Command 'sqlcmd.exe' -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is not installed or sqlcmd.exe is not on PATH.'
}

if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "Seed SQL file was not found: $scriptPath"
}

Write-Host "Starting LocalDB instance MSSQLLocalDB..."
& SqlLocalDB.exe start MSSQLLocalDB | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Unable to start LocalDB. SqlLocalDB exited with code $LASTEXITCODE."
}

Write-Host "Seeding $database on $server..."
& sqlcmd.exe `
    -S $server `
    -E `
    -d $database `
    -b `
    -V 16 `
    -r 1 `
    -i $scriptPath

if ($LASTEXITCODE -ne 0) {
    throw "LocalDB seed failed. sqlcmd exited with code $LASTEXITCODE."
}

Write-Host ''
Write-Host 'Local seed completed.'
Write-Host 'Admin: admin@local.test / Admin123!'
Write-Host 'User:  user@local.test  / User123!'
