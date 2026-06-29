# NAVFAC Tools for Revit 2025

A Revit 2025 add-in for importing, exporting, and comparing NAVFAC drawing index values against a Revit Sheet List schedule.

The main tool is **Import Drawing Numbers**. It reads an `INDEX OF DRAWINGS.csv`, matches the CSV `SHEET` column to each Revit sheet's built-in **Sheet Number**, then writes values into Revit sheet parameters.

Current working import behavior:

```text
CSV NAVFAC DWG. NO.  -> Revit sheet parameter NAVFAC_NO / NAVFAC DWG. NO.
CSV SHEET            -> Revit built-in Sheet Number, used only for matching
CSV NO.              -> Revit sheet parameter NO. / NO / Number / Drawing Number
```

The tool does **not** depend on row order. It matches by sheet number.

## Included tools

- **Import Drawing Numbers**: imports NAVFAC drawing numbers and the `NO.` column from CSV into Revit sheets.
- **Export Sheet Index**: exports Revit sheet numbers, sheet names, and current NAVFAC values to CSV.
- **Compare CSV**: compares a NAVFAC CSV to current Revit sheet values without changing the model.
- **About**: shows version and installation path.

## Best workflow for a working build machine

Use this on the machine that has Revit 2025, the .NET 8 SDK, and the source folder.

```powershell
cd C:\revit-navfac-tools
git pull
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Then restart Revit 2025.

## Create a portable ZIP for other machines

Use this on the working build machine:

```powershell
cd C:\revit-navfac-tools
git pull
powershell -ExecutionPolicy Bypass -File .\package.ps1
```

This creates:

```text
C:\revit-navfac-tools\dist\NAVFACTools-Revit2025-Portable.zip
```

Copy that ZIP to a thumb drive or shared folder.

On another Revit 2025 computer:

```text
1. Copy the ZIP to the computer.
2. Right-click ZIP -> Extract All.
3. Open the extracted folder.
4. Double-click Install_NAVFAC_Tools.bat.
5. Restart Revit 2025.
6. Open the NAVFAC Tools ribbon tab.
```

No Git, Visual Studio, or .NET SDK is required on the target computer when using the portable ZIP.

## Push changes to GitHub when logged in

Use these commands when you are on the source/build machine and Git is already logged in.

Check that Git can reach GitHub:

```powershell
cd C:\revit-navfac-tools
git pull
```

Commit and push normal source/documentation changes:

```powershell
cd C:\revit-navfac-tools
git status
git add .
git commit -m "Update NAVFAC Tools"
git push
```

If there is nothing to commit, Git will say something like `nothing to commit, working tree clean`. That is fine.

## Optional: publish the ZIP into GitHub

Use this only when the build machine is logged into Git and you want the ZIP committed to the repo:

```powershell
cd C:\revit-navfac-tools
git pull
powershell -ExecutionPolicy Bypass -File .\publish-portable-zip.ps1
```

This creates and pushes:

```text
revit-navfac-tools/release/NAVFACTools-Revit2025-Portable.zip
```

Manual version of the same push process:

```powershell
cd C:\revit-navfac-tools
git pull
powershell -ExecutionPolicy Bypass -File .\package.ps1
mkdir release -Force
copy .\dist\NAVFACTools-Revit2025-Portable.zip .\release\NAVFACTools-Revit2025-Portable.zip
git add .\release\NAVFACTools-Revit2025-Portable.zip
git commit -m "Add portable NAVFAC Tools installer ZIP"
git push
```

If you only want a local ZIP to copy to a thumb drive, use `package.ps1` instead.

## User workflow inside Revit

1. Open the Revit model.
2. Open the **NAVFAC Tools** ribbon tab.
3. Run **Compare CSV** first if you want to preview differences without changing Revit.
4. Run **Import Drawing Numbers**.
5. Select the CSV.
6. Review the preview.
7. Click **OK** to update Revit sheet parameters.
8. Review the import report.
9. Run **Compare CSV** again to confirm values match.

Expected successful compare after import:

```text
Same values: matching sheet count
Different values: 0
```

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

Only the build/source machine needs these:

- Revit 2025 installed.
- .NET 8 SDK.
- Visual Studio 2022 or `dotnet build`.

The project references these local Revit files:

```text
C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll
C:\Program Files\Autodesk\Revit 2025\RevitAPIUI.dll
```

## Build only

From `revit-navfac-tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Or directly:

```powershell
cd .\NAVFACTools
dotnet build -c Release
```

## Install from source

From `revit-navfac-tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Then restart Revit.

## Portable install

The portable ZIP contains:

```text
Install_NAVFAC_Tools.bat
portable-install.ps1
NAVFACTools.dll
NAVFACTools.deps.json
QUICK_START.txt
```

Run:

```text
Install_NAVFAC_Tools.bat
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
dist\NAVFACTools-Revit2025-Portable.zip
```

## CSV expectations

The importer and comparer auto-detect these columns by header name:

```text
NAVFAC DWG. NO.
SHEET
NO.
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

Accepted `NO.` header variants include:

```text
NO.
NO
NUMBER
INDEX NO
INDEX NUMBER
```

The importer ignores blank rows, title rows, and discipline heading rows because it only processes rows that have both a sheet number and a nonblank NAVFAC drawing number. It never overwrites a Revit value with a blank CSV value.

## Parameters used

The importer has fallback parameter names so the Notepad/settings edit should not be needed for standard sheets.

For NAVFAC, it tries:

```text
TargetParameterName from settings.json
NAVFAC_NO
NAVFAC DWG. NO.
NAVFAC DWG NO
```

For the `NO.` column, it tries:

```text
DrawingNumberParameterName from settings.json
NO.
NO
Number
Drawing Number
```

Settings are stored here after first run:

```text
%AppData%\NAVFACTools\settings.json
```

Manual settings edits are only needed if a model uses a completely different parameter name.

## Safety behavior

- Matches by sheet number, not row order.
- Imports both the NAVFAC drawing number and the `NO.` column when matching target parameters exist.
- Ignores duplicate sheet numbers in the CSV and reports them.
- Skips sheets missing target parameters and reports them.
- Skips read-only parameters.
- Uses a Revit transaction and rolls back if an unexpected exception occurs.
- Displays a preview before writing changes.
