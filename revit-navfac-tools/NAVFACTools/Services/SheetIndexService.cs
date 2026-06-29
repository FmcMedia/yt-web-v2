using Autodesk.Revit.DB;
using NAVFACTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NAVFACTools.Services;

public sealed class RevitSheetIndexRow
{
    public required string SheetNumber { get; init; }
    public required string SheetName { get; init; }
    public required string NavfacDrawingNumber { get; init; }
    public ElementId SheetElementId { get; init; } = ElementId.InvalidElementId;
}

public sealed class SheetIndexService
{
    private readonly Document _document;

    public SheetIndexService(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    public IReadOnlyList<RevitSheetIndexRow> GetSheets(string targetParameterName)
    {
        return new FilteredElementCollector(_document)
            .OfClass(typeof(ViewSheet))
            .Cast<ViewSheet>()
            .Where(sheet => !sheet.IsPlaceholder)
            .Select(sheet => new RevitSheetIndexRow
            {
                SheetElementId = sheet.Id,
                SheetNumber = sheet.SheetNumber ?? string.Empty,
                SheetName = sheet.Name ?? string.Empty,
                NavfacDrawingNumber = sheet.LookupParameter(targetParameterName)?.AsString() ?? string.Empty
            })
            .OrderBy(row => row.SheetNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyDictionary<string, RevitSheetIndexRow> GetSheetMap(string targetParameterName)
    {
        return GetSheets(targetParameterName)
            .GroupBy(row => CsvDrawingIndexReader.NormalizeSheetNumber(row.SheetNumber))
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }
}
