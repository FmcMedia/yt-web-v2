using NAVFACTools.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NAVFACTools.Services;

public sealed class CsvDrawingIndexReader
{
    private static readonly string[] NavfacHeaderCandidates =
    {
        "NAVFAC DWG. NO.",
        "NAVFAC DWG NO",
        "NAVFAC DRAWING NO",
        "NAVFAC DRAWING NUMBER",
        "DRAWING NO",
        "DRAWING NUMBER"
    };

    private static readonly string[] SheetHeaderCandidates =
    {
        "SHEET",
        "SHEET NUMBER",
        "SHEET NO",
        "SHEET NO."
    };

    private static readonly string[] TitleHeaderCandidates =
    {
        "SHEET TITLE",
        "TITLE"
    };

    public IReadOnlyList<DrawingIndexRow> Read(string csvPath)
    {
        if (string.IsNullOrWhiteSpace(csvPath))
            throw new ArgumentException("CSV path is blank.", nameof(csvPath));

        if (!File.Exists(csvPath))
            throw new FileNotFoundException("CSV file was not found.", csvPath);

        var physicalLines = File.ReadAllLines(csvPath, Encoding.UTF8);
        if (physicalLines.Length == 0)
            return Array.Empty<DrawingIndexRow>();

        var parsed = physicalLines
            .Select((line, index) => new { LineNumber = index + 1, Cells = ParseCsvLine(line) })
            .ToList();

        int headerIndex = FindHeaderIndex(parsed.Select(p => p.Cells).ToList());
        if (headerIndex < 0)
            throw new InvalidOperationException("Could not find CSV headers. Expected columns named 'NAVFAC DWG. NO.' and 'SHEET'.");

        var header = parsed[headerIndex].Cells;
        int navfacIndex = FindColumn(header, NavfacHeaderCandidates);
        int sheetIndex = FindColumn(header, SheetHeaderCandidates);
        int titleIndex = FindColumn(header, TitleHeaderCandidates);

        if (navfacIndex < 0)
            throw new InvalidOperationException("CSV is missing NAVFAC drawing number column.");
        if (sheetIndex < 0)
            throw new InvalidOperationException("CSV is missing SHEET column.");

        var rows = new List<DrawingIndexRow>();

        foreach (var item in parsed.Skip(headerIndex + 1))
        {
            string navfac = GetCell(item.Cells, navfacIndex);
            string sheet = NormalizeSheetNumber(GetCell(item.Cells, sheetIndex));
            string? title = titleIndex >= 0 ? GetCell(item.Cells, titleIndex) : null;

            if (string.IsNullOrWhiteSpace(sheet))
                continue;

            if (string.IsNullOrWhiteSpace(navfac))
                continue;

            rows.Add(new DrawingIndexRow(item.LineNumber, sheet, navfac.Trim(), title));
        }

        return rows;
    }

    private static int FindHeaderIndex(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (FindColumn(rows[i], NavfacHeaderCandidates) >= 0 && FindColumn(rows[i], SheetHeaderCandidates) >= 0)
                return i;
        }

        return -1;
    }

    private static int FindColumn(IReadOnlyList<string> header, IReadOnlyList<string> candidates)
    {
        for (int i = 0; i < header.Count; i++)
        {
            string normalized = NormalizeHeader(header[i]);
            if (candidates.Any(candidate => NormalizeHeader(candidate) == normalized))
                return i;
        }

        return -1;
    }

    private static string GetCell(IReadOnlyList<string> cells, int index)
    {
        if (index < 0 || index >= cells.Count)
            return string.Empty;

        return cells[index].Trim();
    }

    private static string NormalizeHeader(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Replace("\uFEFF", string.Empty)
            .Replace(".", string.Empty)
            .Replace("  ", " ")
            .ToUpperInvariant();
    }

    public static string NormalizeSheetNumber(string value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                cells.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        cells.Add(sb.ToString());
        return cells;
    }
}
