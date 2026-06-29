using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NAVFACTools.Services;
using System;
using System.IO;
using System.Windows.Forms;
using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;

namespace NAVFACTools.Commands;

[Transaction(TransactionMode.Manual)]
public sealed class ExportSheetIndexCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document? document = commandData.Application.ActiveUIDocument?.Document;
        if (document is null)
        {
            message = "No active Revit document is open.";
            return Result.Failed;
        }

        try
        {
            NavfacSettings settings = SettingsService.Load();
            string? path = PickSavePath(settings.LastCsvFolder, document.Title);
            if (string.IsNullOrWhiteSpace(path))
                return Result.Cancelled;

            settings.LastCsvFolder = Path.GetDirectoryName(path);
            SettingsService.Save(settings);

            var service = new SheetIndexService(document);
            var rows = service.GetSheets(settings.TargetParameterName);
            CsvExportWriter.WriteSheetIndex(path, rows);

            RevitTaskDialog.Show("NAVFAC Tools", $"Exported {rows.Count} sheets to:\n\n{path}");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            RevitTaskDialog.Show("NAVFAC Tools - Error", ex.ToString());
            return Result.Failed;
        }
    }

    private static string? PickSavePath(string? initialDirectory, string documentTitle)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export Revit Sheet Index CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = MakeSafeFileName(documentTitle) + "_SheetIndex.csv",
            OverwritePrompt = true,
            RestoreDirectory = true
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            dialog.InitialDirectory = initialDirectory;

        DialogResult result = dialog.ShowDialog();
        return result == DialogResult.OK ? dialog.FileName : null;
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return string.IsNullOrWhiteSpace(value) ? "Revit" : value;
    }
}
