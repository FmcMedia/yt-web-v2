using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Reflection;

namespace NAVFACTools.Commands;

[Transaction(TransactionMode.Manual)]
public sealed class AboutCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        TaskDialog.Show(
            "NAVFAC Tools",
            "NAVFAC Tools for Revit 2025\n\n" +
            $"Version: {version}\n" +
            $"Assembly: {assemblyPath}\n\n" +
            "Tools:\n" +
            "  Import Drawing Numbers\n" +
            "  Export Sheet Index\n" +
            "  Compare CSV\n\n" +
            "Default target sheet parameter:\n" +
            "  NAVFAC DWG. NO.");

        return Result.Succeeded;
    }
}
