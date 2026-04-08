param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $RepoRoot "src\PlateGuard.App\PlateGuard.App.csproj"
$PublishDir = Join-Path $RepoRoot "builds\windows-release\publish"
$DeliveryDir = Join-Path $RepoRoot "builds\windows-release\delivery"
$DeliveryAppDir = Join-Path $DeliveryDir "PlateGuard"

$dotnetCommand = Get-Command dotnet.exe -ErrorAction SilentlyContinue
if (-not $dotnetCommand) {
    $defaultDotnetPath = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $defaultDotnetPath) {
        $DotnetPath = $defaultDotnetPath
    }
    else {
        throw "dotnet.exe was not found. Install the .NET SDK or update the script with the correct path."
    }
}
else {
    $DotnetPath = $dotnetCommand.Source
}

Write-Host "Publishing PlateGuard..." -ForegroundColor Cyan
Write-Host "Project: $ProjectPath"
Write-Host "Output:  $PublishDir"

Remove-Item $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $DeliveryAppDir -Recurse -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $DeliveryDir -Force | Out-Null
New-Item -ItemType Directory -Path $DeliveryAppDir -Force | Out-Null

$publishArgs = @(
    "publish",
    ('"{0}"' -f $ProjectPath),
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=false",
    "-p:PublishTrimmed=false",
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "-p:Version=$Version",
    "-p:FileVersion=1.0.0.0",
    "-o", ('"{0}"' -f $PublishDir)
)

if ($NoRestore) {
    $publishArgs += "--no-restore"
}

$publishProcess = Start-Process -FilePath $DotnetPath -ArgumentList ($publishArgs -join " ") -Wait -NoNewWindow -PassThru
if ($publishProcess.ExitCode -ne 0) {
    throw "dotnet publish failed with exit code $($publishProcess.ExitCode)."
}

$publishedItems = Get-ChildItem -Path $PublishDir -Force
if (-not $publishedItems) {
    throw "Publish folder is empty."
}

foreach ($item in $publishedItems) {
    Copy-Item -Path $item.FullName -Destination $DeliveryAppDir -Recurse -Force
}

Copy-Item (Join-Path $PSScriptRoot "DELIVERY-README.md") (Join-Path $DeliveryDir "README.md") -Force
Copy-Item (Join-Path $PSScriptRoot "ADMIN-NOTES.md") (Join-Path $DeliveryDir "ADMIN-NOTES.md") -Force
Copy-Item (Join-Path $PSScriptRoot "Create-Desktop-Shortcut.ps1") (Join-Path $DeliveryDir "Create-Desktop-Shortcut.ps1") -Force

Write-Host ""
Write-Host "Publish complete." -ForegroundColor Green
Write-Host "Portable app: $DeliveryAppDir"
Write-Host "README:       $(Join-Path $DeliveryDir 'README.md')"
Write-Host "Admin notes:  $(Join-Path $DeliveryDir 'ADMIN-NOTES.md')"
