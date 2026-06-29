using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NAVFACTools.Services;

public static class CsvExportWriter
{
    public static void WriteSheetIndex(string path, IEnumerable<RevitSheetIndexRow> rows)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.WriteLine("NAVFAC DWG. NO.,SHEET,SHEET TITLE");

        foreach (RevitSheetIndexRow row in rows)
        {
            writer.WriteLine(string.Join(",", new[]
            {
                Escape(row.NavfacDrawingNumber),
                Escape(row.SheetNumber),
                Escape(row.SheetName)
            }));
        }
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        bool mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        string escaped = value.Replace("\"", "\"\"");
        return mustQuote ? $"\"{escaped}\"" : escaped;
    }
}
