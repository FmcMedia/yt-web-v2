# NAVFAC Tools for Revit 2025

A Revit 2025 add-in for importing, exporting, and comparing NAVFAC drawing numbers against a Revit Sheet List schedule.

The main tool is **Import Drawing Numbers**. It reads an `INDEX OF DRAWINGS.csv`, matches the CSV `SHEET` column to each Revit sheet's built-in **Sheet Number**, and writes the CSV `NAVFAC DWG. NO.` value into the Revit sheet parameter named **NAVFAC DWG. NO.**

## Included tools

- **Import Drawing Numbers**: imports NAVFAC drawing numbers from CSV into Revit sheets.
- **Export Sheet Index**: exports Revit sheet numbers, sheet names, and current NAVFAC values to CSV.
- **Compare CSV**: compares a NAVFAC CSV to current Revit sheet values without changing the model.
- **About**: shows version and installation path.

## User workflow

1. Run `install.ps1` once.
2. Restart Revit 2025.
3. Open the Revit model.
4. Click **NAVFAC Tools → Import Drawing Numbers**.
5. Select the CSV.
6. Review the preview.
7. Click **OK** to update Revit sheet parameters.
8. Review the import report.

## Folder installed by default

```text
%AppData%\Autodesk\Revit\Addins\2025\NAVFACTools\
```

The installer copies the DLL there and writes this manifest:

```text
%AppData%\Autodesk\Revit\Addins\2025\NAVFACTools.addin
```

The installer writes the `.addin` manifest with an absolute DLL path so Revit does not have to expand environment variables.

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

From `revit-navfac-tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Or directly:

```powershell
cd .\NAVFACTools
dotnet build -c Release
```

## Install

From `revit-navfac-tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Then restart Revit.

## Uninstall

From `revit-navfac-tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

## Package

From `revit-navfac-tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\package.ps1
```

This creates:

```text
dist\NAVFACTools-Revit2025.zip
```

## CSV expectations

The importer and comparer auto-detect these columns by header name:

```text
NAVFAC DWG. NO.
SHEET
```

Accepted NAVFAC header variants include:

```text
NAVFAC DWG NO
NAVFAC DRAWING NO
NAVFAC DRAWING NUMBER
DRAWING NO
DRAWING NUMBER
```

Accepted sheet header variants include:

```text
SHEET
SHEET NUMBER
SHEET NO
SHEET NO.
```

The importer ignores blank rows, title rows, and discipline heading rows because it only processes rows that have both a sheet number and a nonblank NAVFAC drawing number. It never overwrites a Revit value with a blank CSV value.

## Parameter used

Default target Revit sheet parameter:

```text
NAVFAC DWG. NO.
```

The setting is stored here after first run:

```text
%AppData%\NAVFACTools\settings.json
```

You can change `TargetParameterName` in that file if another model uses a different sheet parameter name.

## Safety behavior

- Matches by sheet number, not row order.
- Ignores duplicate sheet numbers in the CSV and reports them.
- Skips sheets missing the target parameter and reports them.
- Skips read-only parameters.
- Uses a Revit transaction and rolls back if an unexpected exception occurs.
- Displays a preview before writing changes.
