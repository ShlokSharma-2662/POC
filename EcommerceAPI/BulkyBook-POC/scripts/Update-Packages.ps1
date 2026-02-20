# Update-Packages.ps1
# Script to manage package updates centrally across the Ecommerce solution

param(
    [Parameter(Mandatory=$false)]
    [string]$PackageName,
    
    [Parameter(Mandatory=$false)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [switch]$List,
    
    [Parameter(Mandatory=$false)]
    [switch]$UpdateAll,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get the solution directory
$SolutionDir = Split-Path -Parent $PSScriptRoot
$PackagesPropsPath = Join-Path $SolutionDir "Directory.Packages.props"

if (-not (Test-Path $PackagesPropsPath)) {
    Write-Error "Directory.Packages.props not found in solution root"
    exit 1
}

function Get-PackageVersions {
    [xml]$PackagesProps = Get-Content $PackagesPropsPath
    $packages = @{}
    
    foreach ($package in $PackagesProps.Project.ItemGroup.PackageVersion) {
        $packages[$package.Include] = $package.Version
    }
    
    return $packages
}

function Update-PackageVersion {
    param(
        [string]$PackageName,
        [string]$Version
    )
    
    [xml]$PackagesProps = Get-Content $PackagesPropsPath
    
    $packageNode = $PackagesProps.Project.ItemGroup.PackageVersion | Where-Object { $_.Include -eq $PackageName }
    
    if ($packageNode) {
        $oldVersion = $packageNode.Version
        $packageNode.Version = $Version
        
        if ($WhatIf) {
            Write-Host "Would update $PackageName from $oldVersion to $Version" -ForegroundColor Yellow
        } else {
            $PackagesProps.Save($PackagesPropsPath)
            Write-Host "Updated $PackageName from $oldVersion to $Version" -ForegroundColor Green
        }
    } else {
        Write-Warning "Package '$PackageName' not found in Directory.Packages.props"
    }
}

function Update-AllPackages {
    Write-Host "Checking for package updates..." -ForegroundColor Cyan
    
    # Get current packages
    $packages = Get-PackageVersions
    
    foreach ($package in $packages.GetEnumerator()) {
        try {
            $latestVersion = Find-Package -Name $package.Key -Source "nuget.org" -AllVersions | 
                            Select-Object -First 1 -ExpandProperty Version
            
            if ($latestVersion -and $latestVersion -ne $package.Value) {
                if ($WhatIf) {
                    Write-Host "Would update $($package.Key) from $($package.Value) to $latestVersion" -ForegroundColor Yellow
                } else {
                    Update-PackageVersion -PackageName $package.Key -Version $latestVersion
                }
            }
        }
        catch {
            Write-Warning "Could not check latest version for $($package.Key): $($_.Exception.Message)"
        }
    }
}

# Main script logic
if ($List) {
    Write-Host "Current package versions:" -ForegroundColor Cyan
    $packages = Get-PackageVersions
    $packages.GetEnumerator() | Sort-Object Name | ForEach-Object {
        Write-Host "  $($_.Key): $($_.Value)" -ForegroundColor White
    }
}
elseif ($UpdateAll) {
    Update-AllPackages
}
elseif ($PackageName -and $Version) {
    Update-PackageVersion -PackageName $PackageName -Version $Version
}
elseif ($PackageName) {
    Write-Host "Usage: .\Update-Packages.ps1 -PackageName <PackageName> -Version <Version>" -ForegroundColor Yellow
    Write-Host "       .\Update-Packages.ps1 -List" -ForegroundColor Yellow
    Write-Host "       .\Update-Packages.ps1 -UpdateAll" -ForegroundColor Yellow
    Write-Host "       .\Update-Packages.ps1 -UpdateAll -WhatIf" -ForegroundColor Yellow
}
else {
    Write-Host "Ecommerce Package Manager Console" -ForegroundColor Cyan
    Write-Host "=================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Available commands:" -ForegroundColor White
    Write-Host "  -List                    : List all current package versions" -ForegroundColor Gray
    Write-Host "  -UpdateAll               : Update all packages to latest versions" -ForegroundColor Gray
    Write-Host "  -UpdateAll -WhatIf       : Preview what would be updated" -ForegroundColor Gray
    Write-Host "  -PackageName <name> -Version <version> : Update specific package" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor White
    Write-Host "  .\Update-Packages.ps1 -List" -ForegroundColor Gray
    Write-Host "  .\Update-Packages.ps1 -UpdateAll -WhatIf" -ForegroundColor Gray
    Write-Host "  .\Update-Packages.ps1 -PackageName 'Microsoft.EntityFrameworkCore' -Version '9.0.4'" -ForegroundColor Gray
}
