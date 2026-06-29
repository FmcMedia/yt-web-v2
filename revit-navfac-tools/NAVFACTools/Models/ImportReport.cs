using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NAVFACTools.Models;

public sealed class ImportReport
{
    public int CsvRowsRead { get; set; }
    public int CandidateRows { get; set; }
    public int RevitSheetsFound { get; set; }
    public int UpdatedCount { get; set; }
    public int UnchangedCount { get; set; }
    public int SkippedLockedOrReadOnlyCount { get; set; }

    public List<string> MissingInRevit { get; } = new();
    public List<string> DuplicateSheetsInCsv { get; } = new();
    public List<string> MissingTargetParameter { get; } = new();
    public List<string> FailedUpdates { get; } = new();
    public List<string> ChangedLines { get; } = new();

    public string ToDialogText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("NAVFAC Drawing Number Import Complete");
        sb.AppendLine();
        sb.AppendLine($"CSV rows read: {CsvRowsRead}");
        sb.AppendLine($"Candidate rows: {CandidateRows}");
        sb.AppendLine($"Revit sheets found: {RevitSheetsFound}");
        sb.AppendLine($"Updated: {UpdatedCount}");
        sb.AppendLine($"Unchanged: {UnchangedCount}");
        sb.AppendLine($"Skipped read-only: {SkippedLockedOrReadOnlyCount}");

        AppendSection(sb, "CSV sheet numbers not found in Revit", MissingInRevit);
        AppendSection(sb, "Duplicate sheet numbers in CSV", DuplicateSheetsInCsv);
        AppendSection(sb, "Sheets missing target parameter", MissingTargetParameter);
        AppendSection(sb, "Failed updates", FailedUpdates);

        if (ChangedLines.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Changed sheets:");
            foreach (string line in ChangedLines.Take(35))
                sb.AppendLine("  " + line);
            if (ChangedLines.Count > 35)
                sb.AppendLine($"  ...and {ChangedLines.Count - 35} more");
        }

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
