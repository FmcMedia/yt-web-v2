using NAVFACTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NAVFACTools.Services;

public static class CompareService
{
    public static CompareReport Compare(
        IReadOnlyList<DrawingIndexRow> csvRows,
        IReadOnlyList<RevitSheetIndexRow> revitRows)
    {
        var report = new CompareReport
        {
            CsvRows = csvRows.Count,
            RevitSheets = revitRows.Count
        };

        var duplicateCsv = csvRows
            .GroupBy(row => CsvDrawingIndexReader.NormalizeSheetNumber(row.SheetNumber))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(value => value)
            .ToList();

        report.DuplicateCsvSheets.AddRange(duplicateCsv);

        Dictionary<string, DrawingIndexRow> csvMap = csvRows
            .GroupBy(row => CsvDrawingIndexReader.NormalizeSheetNumber(row.SheetNumber))
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        Dictionary<string, RevitSheetIndexRow> revitMap = revitRows
            .GroupBy(row => CsvDrawingIndexReader.NormalizeSheetNumber(row.SheetNumber))
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (string sheet in csvMap.Keys.OrderBy(x => x))
        {
            if (!revitMap.TryGetValue(sheet, out RevitSheetIndexRow? revitRow))
            {
                report.CsvOnly.Add(sheet);
                continue;
            }

            string csvValue = csvMap[sheet].NavfacDrawingNumber.Trim();
            string revitValue = (revitRow.NavfacDrawingNumber ?? string.Empty).Trim();

            if (string.Equals(csvValue, revitValue, StringComparison.OrdinalIgnoreCase))
                report.SameValues.Add(sheet);
            else
                report.DifferentValues.Add($"{sheet}: Revit='{revitValue}' CSV='{csvValue}'");
        }

        foreach (string sheet in revitMap.Keys.OrderBy(x => x))
        {
            if (!csvMap.ContainsKey(sheet))
                report.RevitOnly.Add(sheet);
        }

        return report;
    }
}
