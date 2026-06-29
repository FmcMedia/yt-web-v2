using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NAVFACTools.Models;
using NAVFACTools.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RevitTaskDialog = Autodesk.Revit.UI.TaskDialog;
using RevitTaskDialogResult = Autodesk.Revit.UI.TaskDialogResult;

namespace NAVFACTools.Commands;

[Transaction(TransactionMode.Manual)]
public sealed class ImportDrawingNumbersCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiApplication = commandData.Application;
        UIDocument? uiDocument = uiApplication.ActiveUIDocument;
        Document? document = uiDocument?.Document;

        if (document is null)
        {
            message = "No active Revit document is open.";
            return Result.Failed;
        }

        try
        {
            NavfacSettings settings = SettingsService.Load();
            string? csvPath = PickCsv(settings.LastCsvFolder);
            if (string.IsNullOrWhiteSpace(csvPath))
                return Result.Cancelled;

            settings.LastCsvFolder = Path.GetDirectoryName(csvPath);
            SettingsService.Save(settings);

            var reader = new CsvDrawingIndexReader();
            var rows = reader.Read(csvPath);

            if (rows.Count == 0)
            {
                RevitTaskDialog.Show("NAVFAC Tools", "No usable drawing-number rows were found in the selected CSV.");
                return Result.Cancelled;
            }

            string previewText = BuildPreviewText(rows, settings.TargetParameterName);
            RevitTaskDialogResult previewResult = RevitTaskDialog.Show(
                "NAVFAC Tools - Confirm Import",
                previewText,
                TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel);

            if (previewResult != RevitTaskDialogResult.Ok)
                return Result.Cancelled;

            var updater = new SheetNavfacUpdater(document);
            ImportReport report = updater.Update(rows, settings.TargetParameterName);

            RevitTaskDialog.Show("NAVFAC Tools", report.ToDialogText());
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            RevitTaskDialog.Show("NAVFAC Tools - Error", ex.ToString());
            return Result.Failed;
        }
    }

    private static string? PickCsv(string? initialDirectory)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select NAVFAC Drawing Index CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            RestoreDirectory = true
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
            dialog.InitialDirectory = initialDirectory;

        DialogResult result = dialog.ShowDialog();
        return result == DialogResult.OK ? dialog.FileName : null;
    }

    private static string BuildPreviewText(System.Collections.Generic.IReadOnlyList<DrawingIndexRow> rows, string targetParameterName)
    {
        var firstRows = rows.Take(10).Select(r => $"  {r.SheetNumber} → {r.NavfacDrawingNumber}");

        return
            $"Ready to import NAVFAC drawing numbers.\n\n" +
            $"Rows found: {rows.Count}\n" +
            $"Target Revit sheet parameter: {targetParameterName}\n\n" +
            "First rows:\n" +
            string.Join("\n", firstRows) +
            (rows.Count > 10 ? $"\n  ...and {rows.Count - 10} more" : string.Empty) +
            "\n\nClick OK to update the Revit sheets.";
    }
}
