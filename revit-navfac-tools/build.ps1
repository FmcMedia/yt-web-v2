$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Join-Path $scriptRoot "NAVFACTools"

$revitApi = "C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll"
$revitApiUi = "C:\Program Files\Autodesk\Revit 2025\RevitAPIUI.dll"

if (!(Test-Path $revitApi) -or !(Test-Path $revitApiUi)) {
    throw "Revit 2025 API files were not found at C:\Program Files\Autodesk\Revit 2025\."
}

dotnet restore $projectRoot
dotnet build $projectRoot -c Release --no-restore

Write-Host "Build complete."
