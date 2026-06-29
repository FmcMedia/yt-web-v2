using Autodesk.Revit.DB;
using NAVFACTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NAVFACTools.Services;

public sealed class SheetNavfacUpdater
{
    private readonly Document _document;

    public SheetNavfacUpdater(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    public ImportReport Update(IReadOnlyList<DrawingIndexRow> csvRows, string targetParameterName, string drawingNumberParameterName)
    {
        if (string.IsNullOrWhiteSpace(targetParameterName))
            throw new ArgumentException("Target parameter name is blank.", nameof(targetParameterName));

        var report = new ImportReport
        {
            CsvRowsRead = csvRows.Count
        };

        var duplicates = csvRows
            .GroupBy(r => CsvDrawingIndexReader.NormalizeSheetNumber(r.SheetNumber))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(x => x)
            .ToList();

        report.DuplicateSheetsInCsv.AddRange(duplicates);

        var usableRows = csvRows
            .GroupBy(r => CsvDrawingIndexReader.NormalizeSheetNumber(r.SheetNumber))
            .Where(g => g.Count() == 1)
            .Select(g => g.First())
            .Where(r => !string.IsNullOrWhiteSpace(r.NavfacDrawingNumber))
            .ToDictionary(
                r => CsvDrawingIndexReader.NormalizeSheetNumber(r.SheetNumber),
                r => r,
                StringComparer.OrdinalIgnoreCase);

        report.CandidateRows = usableRows.Count;

        var sheets = new FilteredElementCollector(_document)
            .OfClass(typeof(ViewSheet))
            .Cast<ViewSheet>()
            .GroupBy(s => CsvDrawingIndexReader.NormalizeSheetNumber(s.SheetNumber))
            .Where(g => g.Count() == 1)
            .ToDictionary(
                g => g.Key,
                g => g.First(),
                StringComparer.OrdinalIgnoreCase);

        report.RevitSheetsFound = sheets.Count;

        foreach (string csvSheet in usableRows.Keys.OrderBy(x => x))
        {
            if (!sheets.ContainsKey(csvSheet))
                report.MissingInRevit.Add(csvSheet);
        }

        using var transaction = new Transaction(_document, "Import NAVFAC Drawing Numbers");
        transaction.Start();

        try
        {
            foreach (var pair in usableRows.OrderBy(p => p.Key))
            {
                string sheetNumber = pair.Key;
                DrawingIndexRow row = pair.Value;

                if (!sheets.TryGetValue(sheetNumber, out ViewSheet? sheet))
                    continue;

                UpdateStringParameter(
                    sheet,
                    sheetNumber,
                    new[] { targetParameterName, "NAVFAC_NO", "NAVFAC DWG. NO.", "NAVFAC DWG NO" },
                    row.NavfacDrawingNumber,
                    report,
                    "NAVFAC");

                if (!string.IsNullOrWhiteSpace(drawingNumberParameterName) && !string.IsNullOrWhiteSpace(row.DrawingNumber))
                {
                    UpdateStringParameter(
                        sheet,
                        sheetNumber,
                        new[] { drawingNumberParameterName, "NO.", "NO", "Number", "Drawing Number" },
                        row.DrawingNumber!,
                        report,
                        "NO");
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.RollBack();
            throw;
        }

        return report;
    }

    private static void UpdateStringParameter(ViewSheet sheet, string sheetNumber, IEnumerable<string> parameterNames, string newValueRaw, ImportReport report, string label)
    {
        string[] candidates = parameterNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Parameter? parameter = null;
        string? matchedName = null;

        foreach (string candidate in candidates)
        {
            parameter = sheet.LookupParameter(candidate);
            if (parameter is not null)
            {
                matchedName = candidate;
                break;
            }
        }

        if (parameter is null)
        {
            report.MissingTargetParameter.Add($"{sheetNumber}: {string.Join(" / ", candidates)}");
            return;
        }

        if (parameter.IsReadOnly)
        {
            report.SkippedLockedOrReadOnlyCount++;
            return;
        }

        if (parameter.StorageType != StorageType.String)
        {
            report.FailedUpdates.Add($"{sheetNumber}: {matchedName} is {parameter.StorageType}, expected String");
            return;
        }

        string oldValue = parameter.AsString() ?? string.Empty;
        string newValue = newValueRaw.Trim();

        if (string.Equals(oldValue.Trim(), newValue, StringComparison.OrdinalIgnoreCase))
        {
            report.UnchangedCount++;
            return;
        }

        bool success = parameter.Set(newValue);
        if (success)
        {
            report.UpdatedCount++;
            report.ChangedLines.Add($"{sheetNumber} [{label}/{matchedName}]: '{oldValue}' → '{newValue}'");
        }
        else
        {
            report.FailedUpdates.Add($"{sheetNumber}: {matchedName}");
        }
    }
}
