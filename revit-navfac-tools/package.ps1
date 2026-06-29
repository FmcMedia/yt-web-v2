$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Join-Path $scriptRoot "NAVFACTools"
$releaseDir = Join-Path $projectRoot "bin\Release\net8.0-windows"
$packageDir = Join-Path $scriptRoot "dist"
$zipPath = Join-Path $packageDir "NAVFACTools-Revit2025-Portable.zip"

& (Join-Path $scriptRoot "build.ps1")

if (Test-Path $packageDir) {
    Remove-Item $packageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null

$payload = Join-Path $packageDir "NAVFACTools-Revit2025"
New-Item -ItemType Directory -Force -Path $payload | Out-Null

Copy-Item (Join-Path $releaseDir "NAVFACTools.*") $payload -Force
Copy-Item (Join-Path $scriptRoot "Install_NAVFAC_Tools.bat") $payload -Force
Copy-Item (Join-Path $scriptRoot "portable-install.ps1") $payload -Force
Copy-Item (Join-Path $scriptRoot "uninstall.ps1") $payload -Force
Copy-Item (Join-Path $scriptRoot "README.md") $payload -Force

$quickStart = @"
NAVFAC Tools for Revit 2025

Install:
1. Extract this ZIP.
2. Double-click Install_NAVFAC_Tools.bat.
3. Restart Revit 2025.
4. Open the NAVFAC Tools ribbon tab.

No Git, Visual Studio, or .NET SDK is required on the target machine.
"@

Set-Content -Path (Join-Path $payload "QUICK_START.txt") -Value $quickStart -Encoding UTF8
Compress-Archive -Path (Join-Path $payload "*") -DestinationPath $zipPath -Force
Write-Host "Portable package created: $zipPath"
