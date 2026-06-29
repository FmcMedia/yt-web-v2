using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace NAVFACTools;

public sealed class App : IExternalApplication
{
    private const string TabName = "NAVFAC Tools";
    private const string DrawingPanelName = "Drawing Index";
    private const string UtilityPanelName = "Utilities";

    public Result OnStartup(UIControlledApplication application)
    {
        TryCreateRibbonTab(application, TabName);

        RibbonPanel drawingPanel = GetOrCreatePanel(application, TabName, DrawingPanelName);
        RibbonPanel utilityPanel = GetOrCreatePanel(application, TabName, UtilityPanelName);
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        AddButton(
            drawingPanel,
            "NAVFAC_ImportDrawingNumbers",
            "Import\nDrawing Numbers",
            assemblyPath,
            "NAVFACTools.Commands.ImportDrawingNumbersCommand",
            "Import NAVFAC drawing numbers from a CSV and write them to Revit sheet parameters.");

        AddButton(
            drawingPanel,
            "NAVFAC_ExportSheetIndex",
            "Export\nSheet Index",
            assemblyPath,
            "NAVFACTools.Commands.ExportSheetIndexCommand",
            "Export the current Revit sheet index to a CSV file.");

        AddButton(
            drawingPanel,
            "NAVFAC_CompareCsvToRevit",
            "Compare\nCSV",
            assemblyPath,
            "NAVFACTools.Commands.CompareCsvToRevitCommand",
            "Compare a NAVFAC CSV against the current Revit sheets without changing the model.");

        AddButton(
            utilityPanel,
            "NAVFAC_About",
            "About",
            assemblyPath,
            "NAVFACTools.Commands.AboutCommand",
            "Show NAVFAC Tools version and installation information.");

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    private static void AddButton(RibbonPanel panel, string internalName, string displayName, string assemblyPath, string className, string toolTip)
    {
        if (PanelContainsButton(panel, internalName))
            return;

        var buttonData = new PushButtonData(internalName, displayName, assemblyPath, className)
        {
            ToolTip = toolTip
        };

        panel.AddItem(buttonData);
    }

    private static bool PanelContainsButton(RibbonPanel panel, string internalName)
    {
        foreach (RibbonItem item in panel.GetItems())
        {
            if (string.Equals(item.Name, internalName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void TryCreateRibbonTab(UIControlledApplication application, string tabName)
    {
        try
        {
            application.CreateRibbonTab(tabName);
        }
        catch
        {
            // Tab already exists.
        }
    }

    private static RibbonPanel GetOrCreatePanel(UIControlledApplication application, string tabName, string panelName)
    {
        RibbonPanel? existing = FindPanel(application, tabName, panelName);
        if (existing is not null)
            return existing;

        return application.CreateRibbonPanel(tabName, panelName);
    }

    private static RibbonPanel? FindPanel(UIControlledApplication application, string tabName, string panelName)
    {
        foreach (RibbonPanel panel in application.GetRibbonPanels(tabName))
        {
            if (string.Equals(panel.Name, panelName, StringComparison.OrdinalIgnoreCase))
                return panel;
        }

        return null;
    }
}
