param(
  [string]$ProjectKey = "ecommerce-ui",
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

$scannerArgs = @(
  "-Dsonar.projectKey=$ProjectKey",
  "-Dsonar.sources=src",
  "-Dsonar.host.url=$HostUrl",
  "-Dsonar.token=$Token",
  "-Dsonar.sourceEncoding=UTF-8",
  "-Dsonar.exclusions=**/node_modules/**,**/dist/**,**/*.spec.ts"
)

Push-Location "ecommerce-ui"
try {
  npx sonar-scanner @scannerArgs
  exit $LASTEXITCODE
}
finally {
  Pop-Location
}
