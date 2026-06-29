# NAVFAC Tools for Revit 2025

A Revit 2025 add-in for importing NAVFAC drawing numbers from a CSV into a Revit Sheet List schedule.

The first tool is **Import Drawing Numbers**. It reads an `INDEX OF DRAWINGS.csv`, matches the CSV `SHEET` column to each Revit sheet's built-in **Sheet Number**, and writes the CSV `NAVFAC DWG. NO.` value into the Revit sheet parameter named **NAVFAC DWG. NO.**

## User workflow

1. Build the project once.
2. Run `install.ps1`.
3. Restart Revit 2025.
4. Open the model.
5. Click **NAVFAC Tools → Import Drawing Numbers**.
6. Select the CSV.
7. Review the summary.

## Folder installed by default

```text
%AppData%\Autodesk\Revit\Addins\2025\NAVFACTools\
```

The installer copies the DLL there and writes this manifest:

```text
%AppData%\Autodesk\Revit\Addins\2025\NAVFACTools.addin
```

## Build requirements

- Revit 2025 installed on the build machine.
- .NET 8 SDK.
- Visual Studio 2022 or `dotnet build`.

The project references these local Revit files:

```text
C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll
C:\Program Files\Autodesk\Revit 2025\RevitAPIUI.dll
```

## Build

From this folder:

```powershell
cd revit-navfac-tools\NAVFACTools
dotnet build -c Release
```

## Install

From `revit-navfac-tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Then restart Revit.

## CSV expectations

The importer auto-detects these columns by header name:

```text
NAVFAC DWG. NO.
SHEET
```

It ignores blank rows, title rows, and discipline heading rows. It never overwrites a Revit value with a blank CSV value.

## Parameter used

Default target Revit sheet parameter:

```text
NAVFAC DWG. NO.
```

The setting is stored here after first run:

```text
%AppData%\NAVFACTools\settings.json
```
