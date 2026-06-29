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
- Installer, uninstaller, build script, and package script.

## Review result

The code structure is appropriate for a Revit 2025 external application targeting .NET 8 and referencing local Revit 2025 API assemblies.

The importer matches by sheet number, not row order. It avoids overwriting with blank CSV values. It reports duplicates, missing sheets, missing parameters, read-only parameters, unchanged values, and changed values.

The updater uses a Revit transaction and rolls back on unexpected exceptions.

The installer now writes the `.addin` manifest using an absolute DLL path instead of relying on `%AppData%` expansion inside the manifest.

## Corrections made during review

- Added export, compare, and about commands to the ribbon.
- Added build, install, uninstall, and package scripts.
- Changed the installer to generate a manifest with an absolute assembly path.
- Included placeholder sheets in sheet collection, because Sheet List schedules may include placeholder sheets.
- Added validation that the target parameter storage type is `String` before writing.
- Added duplicate protection for CSV rows and Revit sheet-number mapping.
- Added explicit CSV escaping for exports.
- Added settings persistence for the target parameter name and last CSV folder.

## Important assumptions

- Revit 2025 is installed at:

```text
C:\Program Files\Autodesk\Revit 2025\
```

- The Revit sheet parameter is a text/string parameter named:

```text
NAVFAC DWG. NO.
```

- The CSV contains headers that can be detected as:

```text
NAVFAC DWG. NO.
SHEET
```

or one of the supported variants documented in `README.md`.

## Not verified in this environment

This repository was not compiled in the current environment because the available runtime does not include the .NET SDK. The code is set up to compile on a Windows machine with Revit 2025 and .NET 8 installed.

Command to verify on the Revit workstation:

```powershell
cd revit-navfac-tools
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Then install:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

## First test plan inside Revit

1. Open a copy of the project model, not the production model.
2. Run **NAVFAC Tools → Export Sheet Index** to confirm the add-in loads and can read sheets.
3. Run **NAVFAC Tools → Compare CSV** using the NAVFAC CSV.
4. Review the comparison report.
5. Run **NAVFAC Tools → Import Drawing Numbers**.
6. Verify the **INDEX OF DRAWINGS** schedule updates.
7. Save only after the schedule is confirmed.

## Known limitations

- CSV parser supports normal CSV quoting but not multiline quoted fields. This is acceptable for drawing index CSV files.
- Import does not currently create missing sheets.
- Import does not currently create the `NAVFAC DWG. NO.` parameter if it is absent.
- Import skips duplicate CSV sheet numbers rather than guessing which duplicate is correct.
- Import skips non-string target parameters.
