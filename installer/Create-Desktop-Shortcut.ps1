$ErrorActionPreference = "Stop"

$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppExePath = Join-Path $ScriptDirectory "PlateGuard\PlateGuard.exe"

if (-not (Test-Path $AppExePath)) {
    throw "PlateGuard.exe was not found next to this script."
}

$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "PlateGuard.lnk"

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $AppExePath
$shortcut.WorkingDirectory = Split-Path $AppExePath -Parent
$shortcut.IconLocation = "$AppExePath,0"
$shortcut.Save()

Write-Host "Desktop shortcut created: $shortcutPath" -ForegroundColor Green
