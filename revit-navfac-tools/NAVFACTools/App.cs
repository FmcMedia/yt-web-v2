using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace NAVFACTools;

public sealed class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        const string tabName = "NAVFAC Tools";
        const string panelName = "Drawing Index";

        try
        {
            application.CreateRibbonTab(tabName);
        }
        catch
        {
            // Tab already exists.
        }

        RibbonPanel panel;
        try
        {
            panel = application.CreateRibbonPanel(tabName, panelName);
        }
        catch
        {
            panel = FindPanel(application, tabName, panelName) ?? application.CreateRibbonPanel(tabName, panelName);
        }

        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        var importButton = new PushButtonData(
            "NAVFAC_ImportDrawingNumbers",
            "Import\nDrawing Numbers",
            assemblyPath,
            "NAVFACTools.Commands.ImportDrawingNumbersCommand")
        {
            ToolTip = "Import NAVFAC drawing numbers from a CSV and write them to Revit sheet parameters."
        };

        panel.AddItem(importButton);

        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
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
