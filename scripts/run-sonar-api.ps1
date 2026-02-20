param(
  [string]$ProjectKey = "ecommerce-api",
  [string]$HostUrl = "",
  [string]$Token = ""
)

if ([string]::IsNullOrWhiteSpace($HostUrl)) {
  $HostUrl = if ($env:SONAR_HOST_URL) { $env:SONAR_HOST_URL } else { "http://localhost:9000" }
}

if ([string]::IsNullOrWhiteSpace($Token)) {
  $Token = $env:SONAR_TOKEN
}

if ([string]::IsNullOrWhiteSpace($Token)) {
  throw "SONAR_TOKEN is required. Set it in .env or pass -Token."
}

$solutionPath = "EcommerceAPI/BulkyBook-POC/Ecommerce.sln"

Write-Host "Installing/Updating dotnet-sonarscanner..."
dotnet tool update --global dotnet-sonarscanner | Out-Null

$scanner = Join-Path $env:USERPROFILE ".dotnet/tools/dotnet-sonarscanner"

Write-Host "Starting SonarQube analysis for $ProjectKey at $HostUrl"
& $scanner begin /k:"$ProjectKey" /d:sonar.host.url="$HostUrl" /d:sonar.token="$Token"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $solutionPath -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $scanner end /d:sonar.token="$Token"
exit $LASTEXITCODE
