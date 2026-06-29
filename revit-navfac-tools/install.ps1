$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Join-Path $scriptRoot "NAVFACTools"
$releaseDll = Join-Path $projectRoot "bin\Release\net8.0-windows\NAVFACTools.dll"
$manifestSource = Join-Path $scriptRoot "Manifest\NAVFACTools.addin"

$revitAddins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2025"
$installDir = Join-Path $revitAddins "NAVFACTools"
$manifestTarget = Join-Path $revitAddins "NAVFACTools.addin"

if (!(Test-Path $releaseDll)) {
    Write-Host "Release DLL not found. Building project..."
    dotnet build $projectRoot -c Release
}

if (!(Test-Path $releaseDll)) {
    throw "Build failed. Could not find $releaseDll"
}

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item $releaseDll (Join-Path $installDir "NAVFACTools.dll") -Force
Copy-Item $manifestSource $manifestTarget -Force

Write-Host "NAVFAC Tools installed."
Write-Host "DLL:      $installDir\NAVFACTools.dll"
Write-Host "Manifest: $manifestTarget"
Write-Host "Restart Revit 2025, then use NAVFAC Tools > Import Drawing Numbers."
