$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Join-Path $scriptRoot "NAVFACTools"
$releaseDir = Join-Path $projectRoot "bin\Release\net8.0-windows"
$releaseDll = Join-Path $releaseDir "NAVFACTools.dll"

$revitAddins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2025"
$installDir = Join-Path $revitAddins "NAVFACTools"
$manifestTarget = Join-Path $revitAddins "NAVFACTools.addin"
$installedDll = Join-Path $installDir "NAVFACTools.dll"

$revitApi = "C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll"
$revitApiUi = "C:\Program Files\Autodesk\Revit 2025\RevitAPIUI.dll"

if (!(Test-Path $revitApi) -or !(Test-Path $revitApiUi)) {
    throw "Revit 2025 API files were not found at C:\Program Files\Autodesk\Revit 2025\. Install Revit 2025 or adjust NAVFACTools.csproj references."
}

Write-Host "Building NAVFAC Tools..."
dotnet build $projectRoot -c Release

if (!(Test-Path $releaseDll)) {
    throw "Build failed. Could not find $releaseDll"
}

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item (Join-Path $releaseDir "NAVFACTools.*") $installDir -Force

$assemblyPath = $installedDll.Replace("&", "&amp;")
$manifest = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>NAVFAC Tools</Name>
    <Assembly>$assemblyPath</Assembly>
    <AddInId>9C8B06E1-2060-4A6B-9A0F-1133B1735D4B</AddInId>
    <FullClassName>NAVFACTools.App</FullClassName>
    <VendorId>FMC</VendorId>
    <VendorDescription>FMC Media Services</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

Set-Content -Path $manifestTarget -Value $manifest -Encoding UTF8

Write-Host ""
Write-Host "NAVFAC Tools installed."
Write-Host "DLL:      $installedDll"
Write-Host "Manifest: $manifestTarget"
Write-Host ""
Write-Host "Restart Revit 2025, then use NAVFAC Tools > Import Drawing Numbers."
