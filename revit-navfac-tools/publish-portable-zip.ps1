$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$zipPath = Join-Path $scriptRoot "dist\NAVFACTools-Revit2025-Portable.zip"
$releaseDir = Join-Path $scriptRoot "release"
$releaseZip = Join-Path $releaseDir "NAVFACTools-Revit2025-Portable.zip"

& (Join-Path $scriptRoot "package.ps1")

if (!(Test-Path $zipPath)) {
    throw "Package was not created: $zipPath"
}

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
Copy-Item $zipPath $releaseZip -Force

Push-Location $scriptRoot
try {
    git add "release/NAVFACTools-Revit2025-Portable.zip"
    git commit -m "Add portable NAVFAC Tools installer ZIP"
    git push
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Portable ZIP committed and pushed:"
Write-Host $releaseZip
