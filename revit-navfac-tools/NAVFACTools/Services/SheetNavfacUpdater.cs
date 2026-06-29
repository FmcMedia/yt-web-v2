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

    public ImportReport Update(IReadOnlyList<DrawingIndexRow> csvRows, string targetParameterName)
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
            .Where(s => !s.IsPlaceholder)
            .ToDictionary(
                s => CsvDrawingIndexReader.NormalizeSheetNumber(s.SheetNumber),
                s => s,
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

                Parameter? parameter = sheet.LookupParameter(targetParameterName);
                if (parameter is null)
                {
                    report.MissingTargetParameter.Add(sheetNumber);
                    continue;
                }

                if (parameter.IsReadOnly)
                {
                    report.SkippedLockedOrReadOnlyCount++;
                    continue;
                }

                string oldValue = parameter.AsString() ?? string.Empty;
                string newValue = row.NavfacDrawingNumber.Trim();

                if (string.Equals(oldValue.Trim(), newValue, StringComparison.OrdinalIgnoreCase))
                {
                    report.UnchangedCount++;
                    continue;
                }

                bool success = parameter.Set(newValue);
                if (success)
                {
                    report.UpdatedCount++;
                    report.ChangedLines.Add($"{sheetNumber}: '{oldValue}' → '{newValue}'");
                }
                else
                {
                    report.FailedUpdates.Add(sheetNumber);
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
}
