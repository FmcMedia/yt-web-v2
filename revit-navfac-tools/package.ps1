$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Join-Path $scriptRoot "NAVFACTools"
$releaseDir = Join-Path $projectRoot "bin\Release\net8.0-windows"
$packageDir = Join-Path $scriptRoot "dist"
$zipPath = Join-Path $packageDir "NAVFACTools-Revit2025.zip"

& (Join-Path $scriptRoot "build.ps1")

if (Test-Path $packageDir) {
    Remove-Item $packageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null

$payload = Join-Path $packageDir "NAVFACTools-Revit2025"
New-Item -ItemType Directory -Force -Path $payload | Out-Null

Copy-Item (Join-Path $releaseDir "NAVFACTools.*") $payload -Force
Copy-Item (Join-Path $scriptRoot "install.ps1") $payload -Force
Copy-Item (Join-Path $scriptRoot "uninstall.ps1") $payload -Force
Copy-Item (Join-Path $scriptRoot "README.md") $payload -Force

Compress-Archive -Path $payload -DestinationPath $zipPath -Force
Write-Host "Package created: $zipPath"
