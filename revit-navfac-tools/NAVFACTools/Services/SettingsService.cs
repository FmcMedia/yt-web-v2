using System;
using System.IO;
using System.Text.Json;

namespace NAVFACTools.Services;

public sealed class NavfacSettings
{
    public string TargetParameterName { get; set; } = "NAVFAC DWG. NO.";
    public string DrawingNumberParameterName { get; set; } = "NO.";
    public string? LastCsvFolder { get; set; }
}

public static class SettingsService
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NAVFACTools");

    private static readonly string SettingsPath = Path.Combine(SettingsFolder, "settings.json");

    public static NavfacSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new NavfacSettings();

            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<NavfacSettings>(json) ?? new NavfacSettings();
        }
        catch
        {
            return new NavfacSettings();
        }
    }

    public static void Save(NavfacSettings settings)
    {
        Directory.CreateDirectory(SettingsFolder);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, options));
    }
}
