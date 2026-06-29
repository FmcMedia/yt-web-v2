using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAVFACTools.Models;

public sealed class CompareReport
{
    public int CsvRows { get; set; }
    public int RevitSheets { get; set; }
    public List<string> CsvOnly { get; } = new();
    public List<string> RevitOnly { get; } = new();
    public List<string> DifferentValues { get; } = new();
    public List<string> SameValues { get; } = new();
    public List<string> DuplicateCsvSheets { get; } = new();

    public string ToDialogText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("NAVFAC CSV Comparison Complete");
        sb.AppendLine();
        sb.AppendLine($"CSV rows: {CsvRows}");
        sb.AppendLine($"Revit sheets: {RevitSheets}");
        sb.AppendLine($"Same values: {SameValues.Count}");
        sb.AppendLine($"Different values: {DifferentValues.Count}");
        sb.AppendLine($"CSV-only sheets: {CsvOnly.Count}");
        sb.AppendLine($"Revit-only sheets: {RevitOnly.Count}");
        sb.AppendLine($"Duplicate CSV sheets: {DuplicateCsvSheets.Count}");

        AppendSection(sb, "Different values", DifferentValues);
        AppendSection(sb, "CSV-only sheets", CsvOnly);
        AppendSection(sb, "Revit-only sheets", RevitOnly);
        AppendSection(sb, "Duplicate CSV sheets", DuplicateCsvSheets);

        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string title, IReadOnlyCollection<string> values)
    {
        if (values.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine(title + ":");
        foreach (string value in values.Take(25))
            sb.AppendLine("  " + value);
        if (values.Count > 25)
            sb.AppendLine($"  ...and {values.Count - 25} more");
    }
}
