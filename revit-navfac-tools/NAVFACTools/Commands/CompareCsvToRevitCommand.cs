using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NAVFACTools.Models;
using NAVFACTools.Services;
using System;
using System.IO;
using System.Windows.Forms;

namespace NAVFACTools.Commands;

[Transaction(TransactionMode.Manual)]
public sealed class CompareCsvToRevitCommand : IExternalCommand
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
            string? csvPath = PickCsv(settings.LastCsvFolder);
            if (string.IsNullOrWhiteSpace(csvPath))
                return Result.Cancelled;

            settings.LastCsvFolder = Path.GetDirectoryName(csvPath);
            SettingsService.Save(settings);

            var reader = new CsvDrawingIndexReader();
            var csvRows = reader.Read(csvPath);

            var sheetService = new SheetIndexService(document);
            var revitRows = sheetService.GetSheets(settings.TargetParameterName);

            CompareReport report = CompareService.Compare(csvRows, revitRows);
            TaskDialog.Show("NAVFAC Tools", report.ToDialogText());
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("NAVFAC Tools - Error", ex.ToString());
            return Result.Failed;
        }
    }

    private static string? PickCsv(string? initialDirectory)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select NAVFAC Drawing Index CSV to Compare",
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
}
