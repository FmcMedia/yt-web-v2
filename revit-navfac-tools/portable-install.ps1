$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$dllSource = Join-Path $scriptRoot "NAVFACTools.dll"

if (!(Test-Path $dllSource)) {
    throw "NAVFACTools.dll was not found next to this installer. Keep the ZIP contents together and run Install_NAVFAC_Tools.bat from the extracted folder."
}

$revitAddins = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2025"
$installDir = Join-Path $revitAddins "NAVFACTools"
$manifestTarget = Join-Path $revitAddins "NAVFACTools.addin"
$installedDll = Join-Path $installDir "NAVFACTools.dll"

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item (Join-Path $scriptRoot "NAVFACTools.*") $installDir -Force

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
Write-Host "Restart Revit 2025, then open the NAVFAC Tools ribbon tab."
