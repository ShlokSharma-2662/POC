[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Low')]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$sendGridDirectory = Join-Path $repositoryRoot '.secrets'
$localEnvironmentFile = Join-Path $repositoryRoot '.env.local'
$dockerEnvironmentFile = Join-Path $repositoryRoot '.env'

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCommand) {
    throw '.NET SDK was not found on PATH.'
}
$dotnetPath = $dotnetCommand.Source

function Assert-NotPlaceholder {
    param(
        [Parameter(Mandatory)]
        [string] $Value,

        [Parameter(Mandatory)]
        [string] $Name
    )

    if ($Value -match '(?i)(change[ _-]?me|replace[ _-]?me|set[ _-]?in|your[ _-])') {
        throw "'$Name' still contains a placeholder value."
    }
}

function Get-RequiredFileValue {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required local secret file is missing: $Path"
    }

    $value = (Get-Content -LiteralPath $Path -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required local secret file is empty: $Path"
    }

    if ($value -match "[`r`n]") {
        throw "Local secret files must contain exactly one value: $Path"
    }

    return $value
}

function Get-RequiredDotEnvValue {
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Name
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required local environment file is missing: $Path"
    }

    $matches = @()
    foreach ($rawLine in Get-Content -LiteralPath $Path) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) {
            continue
        }

        if ($line.StartsWith('export ', [StringComparison]::OrdinalIgnoreCase)) {
            $line = $line.Substring(7).TrimStart()
        }

        $separatorIndex = $line.IndexOf('=')
        if ($separatorIndex -lt 1) {
            continue
        }

        $candidateName = $line.Substring(0, $separatorIndex).Trim()
        if (-not $candidateName.Equals($Name, [StringComparison]::Ordinal)) {
            continue
        }

        $value = $line.Substring($separatorIndex + 1).Trim()
        if ($value.Length -ge 2 -and
            (($value.StartsWith('"') -and $value.EndsWith('"')) -or
             ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        $matches += $value
    }

    if ($matches.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string] $matches[0])) {
        throw "Required value '$Name' is missing from $Path"
    }

    if ($matches.Count -gt 1) {
        throw "Required value '$Name' is defined more than once in $Path"
    }

    return ([string] $matches[0]).Trim()
}

function Get-UserSecretsId {
    param(
        [Parameter(Mandatory)]
        [string] $ProjectPath
    )

    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) {
        throw "Backend project was not found: $ProjectPath"
    }

    [xml] $projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    $nodes = @($projectXml.SelectNodes("//*[local-name()='UserSecretsId']"))
    if ($nodes.Count -ne 1 -or [string]::IsNullOrWhiteSpace($nodes[0].InnerText)) {
        throw "Expected exactly one UserSecretsId in $ProjectPath"
    }

    return $nodes[0].InnerText.Trim()
}

$sendGridApiKey = Get-RequiredFileValue (Join-Path $sendGridDirectory 'SendGrid__ApiKey')
$sendGridFromEmail = Get-RequiredFileValue (Join-Path $sendGridDirectory 'SendGrid__FromEmail')
$sendGridFromName = Get-RequiredFileValue (Join-Path $sendGridDirectory 'SendGrid__FromName')
$sendGridTemplateId = Get-RequiredFileValue (Join-Path $sendGridDirectory 'SendGrid__OrderConfirmationTemplateId')
$stripeSecretKey = Get-RequiredDotEnvValue $localEnvironmentFile 'STRIPE_SECRET_KEY'
$stripePublishableKey = Get-RequiredDotEnvValue $localEnvironmentFile 'STRIPE_PUBLISHABLE_KEY'
$jwtSecretKey = Get-RequiredDotEnvValue $dockerEnvironmentFile 'JWT_SECRET_KEY'

$valuesToCheck = [ordered] @{
    'SendGrid API key' = $sendGridApiKey
    'SendGrid sender email' = $sendGridFromEmail
    'SendGrid sender name' = $sendGridFromName
    'SendGrid template ID' = $sendGridTemplateId
    'Stripe secret key' = $stripeSecretKey
    'Stripe publishable key' = $stripePublishableKey
    'JWT secret key' = $jwtSecretKey
}

foreach ($entry in $valuesToCheck.GetEnumerator()) {
    Assert-NotPlaceholder -Value $entry.Value -Name $entry.Key
}

if ($sendGridApiKey -notmatch '^SG\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$') {
    throw 'The SendGrid API key does not have the expected format.'
}

if ($sendGridTemplateId -notmatch '^d-[a-fA-F0-9]{32}$') {
    throw 'The SendGrid order-confirmation template ID does not have the expected format.'
}

try {
    $mailAddress = [System.Net.Mail.MailAddress]::new($sendGridFromEmail)
    if (-not $mailAddress.Address.Equals($sendGridFromEmail, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Address contains an unexpected display name.'
    }
}
catch {
    throw 'The SendGrid sender email is invalid.'
}

if ($stripeSecretKey -notmatch '^sk_(test|live)_[A-Za-z0-9]+$') {
    throw 'The Stripe secret key does not have the expected format.'
}

if ($stripePublishableKey -notmatch '^pk_(test|live)_[A-Za-z0-9]+$') {
    throw 'The Stripe publishable key does not have the expected format.'
}

$stripeSecretMode = [regex]::Match($stripeSecretKey, '^sk_(test|live)_').Groups[1].Value
$stripePublishableMode = [regex]::Match($stripePublishableKey, '^pk_(test|live)_').Groups[1].Value
if (-not $stripeSecretMode.Equals($stripePublishableMode, [StringComparison]::Ordinal)) {
    throw 'The Stripe secret and publishable keys must both use the same test/live mode.'
}

if ($jwtSecretKey.Length -lt 32) {
    throw 'JWT_SECRET_KEY must contain at least 32 characters.'
}

$projects = @(
    [ordered] @{
        Name = 'Ecommerce.API'
        Path = Join-Path $repositoryRoot 'EcommerceAPI\BulkyBook-POC\Ecommerce.API\Ecommerce.API.csproj'
    },
    [ordered] @{
        Name = 'Ecommerce.OrderService'
        Path = Join-Path $repositoryRoot 'EcommerceAPI\BulkyBook-POC\Ecommerce.OrderService\Ecommerce.OrderService.csproj'
    },
    [ordered] @{
        Name = 'Ecommerce.ProductService'
        Path = Join-Path $repositoryRoot 'EcommerceAPI\BulkyBook-POC\Ecommerce.ProductService\Ecommerce.ProductService.csproj'
    }
)

$userSecretsIds = [System.Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
foreach ($project in $projects) {
    $userSecretsId = Get-UserSecretsId -ProjectPath $project.Path
    if (-not $userSecretsIds.Add($userSecretsId)) {
        throw "UserSecretsId '$userSecretsId' is shared by more than one backend project."
    }

    $project['UserSecretsId'] = $userSecretsId
}

$configuration = [ordered] @{
    'Jwt:SecretKey' = $jwtSecretKey
    'SendGrid:ApiKey' = $sendGridApiKey
    'SendGrid:FromEmail' = $sendGridFromEmail
    'SendGrid:FromName' = $sendGridFromName
    'SendGrid:OrderConfirmationTemplateId' = $sendGridTemplateId
    'Stripe:SecretKey' = $stripeSecretKey
    'Stripe:PublishableKey' = $stripePublishableKey
}
$configurationJson = $configuration | ConvertTo-Json -Compress
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)

$configuredProjectCount = 0
foreach ($project in $projects) {
    $action = 'replace the seven managed local User Secrets'
    if (-not $PSCmdlet.ShouldProcess($project.Name, $action)) {
        continue
    }

    if ($project.Path.Contains('"')) {
        throw "Backend project path contains an unsupported quote character: $($project.Path)"
    }

    $userSecretsDirectory = Join-Path (
        [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
    ) "Microsoft\UserSecrets\$($project.UserSecretsId)"
    [void] [System.IO.Directory]::CreateDirectory($userSecretsDirectory)

    $inputPath = Join-Path $userSecretsDirectory (
        '.import-' + [System.IO.Path]::GetRandomFileName()
    )
    $outputPath = Join-Path $userSecretsDirectory (
        '.output-' + [System.IO.Path]::GetRandomFileName()
    )
    $errorPath = Join-Path $userSecretsDirectory (
        '.error-' + [System.IO.Path]::GetRandomFileName()
    )

    try {
        [System.IO.File]::WriteAllText($inputPath, $configurationJson, $utf8WithoutBom)
        $arguments = @(
            'user-secrets'
            'set'
            '--project'
            '"' + $project.Path + '"'
        )
        $process = Start-Process `
            -FilePath $dotnetPath `
            -ArgumentList $arguments `
            -RedirectStandardInput $inputPath `
            -RedirectStandardOutput $outputPath `
            -RedirectStandardError $errorPath `
            -WindowStyle Hidden `
            -Wait `
            -PassThru
        $exitCode = $process.ExitCode
    }
    finally {
        foreach ($temporaryPath in @($inputPath, $outputPath, $errorPath)) {
            if (Test-Path -LiteralPath $temporaryPath -PathType Leaf) {
                Remove-Item -LiteralPath $temporaryPath -Force
            }
        }
    }

    if ($exitCode -ne 0) {
        throw "Failed to configure local User Secrets for $($project.Name)."
    }

    $configuredProjectCount++
    Write-Host "Configured seven local User Secrets for $($project.Name)."
}

if ($configuredProjectCount -eq $projects.Count) {
    Write-Host 'Local backend secrets now match the Docker SendGrid, Stripe, and JWT configuration.'
}
else {
    Write-Host 'Validated the local secret sources; no secret values were changed.'
}
