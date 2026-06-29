# NAVFAC Tools Code Review

Review date: 2026-06-29

## Scope reviewed

- Revit external application startup and ribbon creation.
- Import Drawing Numbers command.
- CSV parsing and header detection.
- Revit sheet parameter update transaction.
- Export Sheet Index command.
- Compare CSV command.
- Settings persistence.
- Installer, portable installer, uninstaller, build script, package script, and publish script.

## Review result

The code structure is appropriate for a Revit 2025 external application targeting .NET 8 and referencing local Revit 2025 API assemblies.

The importer matches by sheet number, not row order. It avoids overwriting with blank CSV values. It reports duplicates, missing sheets, missing parameters, read-only parameters, unchanged values, and changed values.

The updater uses a Revit transaction and rolls back on unexpected exceptions.

The installer writes the `.addin` manifest using an absolute DLL path instead of relying on `%AppData%` expansion inside the manifest.

The latest workflow supports a portable ZIP installer so target machines do not need Git, Visual Studio, or the .NET SDK.

## Corrections made during review

- Added export, compare, and about commands to the ribbon.
- Added build, install, uninstall, package, portable install, and publish scripts.
- Changed the installer to generate a manifest with an absolute assembly path.
- Included placeholder sheets in sheet collection, because Sheet List schedules may include placeholder sheets.
- Added validation that target parameter storage type is `String` before writing.
- Added duplicate protection for CSV rows and Revit sheet-number mapping.
- Added explicit CSV escaping for exports.
- Added settings persistence for the target parameter name and last CSV folder.
- Added support for importing CSV column `NO.` into the matching Revit sheet parameter.
- Added fallback target parameter names so standard sheets should not require manual editing of `%AppData%\NAVFACTools\settings.json`.
- Added portable ZIP packaging for easy installation on other machines.

## Current working import mapping

```text
CSV NAVFAC DWG. NO.  -> Revit sheet parameter NAVFAC_NO / NAVFAC DWG. NO. / NAVFAC DWG NO
CSV SHEET            -> Revit built-in Sheet Number, used for matching
CSV NO.              -> Revit sheet parameter NO. / NO / Number / Drawing Number
```

## Important assumptions

- Revit 2025 is installed at:

```text
C:\Program Files\Autodesk\Revit 2025\
```

- Standard project sheets use the same parameter set across machines.
- The CSV contains headers that can be detected as:

```text
NAVFAC DWG. NO.
SHEET
NO.
```

or one of the supported variants documented in `README.md`.

## Verified in user workflow

- The add-in compiled and installed on a Revit 2025 workstation.
- The ribbon tab appeared in Revit as **NAVFAC Tools**.
- The importer originally failed when targeting the schedule header `NAVFAC DWG. NO.` because the actual sheet parameter was `NAVFAC_NO`.
- After setting the target to `NAVFAC_NO`, the NAVFAC import worked.
- The code now includes fallback parameter names so a standard model should work on another machine without the manual Notepad/settings edit.

## Commands

Install/update from source on the working build machine:

```powershell
cd C:\revit-navfac-tools
git pull
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Create a local portable ZIP for thumb drive installation:

```powershell
cd C:\revit-navfac-tools
git pull
powershell -ExecutionPolicy Bypass -File .\package.ps1
```

Output:

```text
C:\revit-navfac-tools\dist\NAVFACTools-Revit2025-Portable.zip
```

Create and push the ZIP into GitHub when Git authentication is available:

```powershell
cd C:\revit-navfac-tools
git pull
powershell -ExecutionPolicy Bypass -File .\publish-portable-zip.ps1
```

## First test plan inside Revit

1. Open a copy of the project model, not the production model.
2. Run **NAVFAC Tools → Compare CSV** using the NAVFAC CSV.
3. Review the comparison report.
4. Run **NAVFAC Tools → Import Drawing Numbers**.
5. Verify the **INDEX OF DRAWINGS** schedule updates.
6. Run **Compare CSV** again to confirm `Different values: 0` for matching sheets.
7. Save only after the schedule is confirmed.

## Known limitations

- CSV parser supports normal CSV quoting but not multiline quoted fields. This is acceptable for drawing index CSV files.
- Import does not currently create missing sheets.
- Import does not currently create missing Revit parameters if they are absent.
- Import skips duplicate CSV sheet numbers rather than guessing which duplicate is correct.
- Import skips non-string target parameters.
- Compare currently focuses on NAVFAC values; the importer also writes `NO.` values.
