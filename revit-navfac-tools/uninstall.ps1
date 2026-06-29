$ErrorActionPreference = "Stop"

$revitAddins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2025"
$installDir = Join-Path $revitAddins "NAVFACTools"
$manifestTarget = Join-Path $revitAddins "NAVFACTools.addin"

if (Test-Path $manifestTarget) {
    Remove-Item $manifestTarget -Force
    Write-Host "Removed $manifestTarget"
}

if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force
    Write-Host "Removed $installDir"
}

Write-Host "NAVFAC Tools uninstalled. Restart Revit 2025 if it is open."
