param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [string]$IsccPath
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$PublishDir = Join-Path $RepoRoot "builds\windows-release\publish"
$InstallerScriptPath = Join-Path $PSScriptRoot "PlateGuard.iss"
$PublishScriptPath = Join-Path $PSScriptRoot "publish-windows.ps1"
$ExpectedExePath = Join-Path $PublishDir "PlateGuard.exe"

if (-not (Test-Path $ExpectedExePath)) {
    Write-Host "Publish output not found. Running publish first..." -ForegroundColor Yellow
    & powershell.exe -ExecutionPolicy Bypass -File $PublishScriptPath -Configuration $Configuration -Runtime $Runtime -Version $Version
    if ($LASTEXITCODE -ne $null -and $LASTEXITCODE -ne 0) {
        throw "Publish script failed with exit code $LASTEXITCODE."
    }
    if (-not $?) {
        throw "Publish script failed."
    }
}

if (-not $IsccPath) {
    $defaultPaths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )

    $IsccPath = $defaultPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $IsccPath) {
    $isccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($isccCommand) {
        $IsccPath = $isccCommand.Source
    }
}

if (-not $IsccPath) {
    throw "Inno Setup compiler was not found. Install Inno Setup 6 or pass -IsccPath."
}

Write-Host "Building Inno Setup installer..." -ForegroundColor Cyan
& $IsccPath $InstallerScriptPath
if ($LASTEXITCODE -ne $null -and $LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}
if (-not $?) {
    throw "Inno Setup compiler failed."
}

Write-Host ""
Write-Host "Installer build complete." -ForegroundColor Green
Write-Host "Output folder: $(Join-Path $RepoRoot 'builds\windows-release\installer')"
