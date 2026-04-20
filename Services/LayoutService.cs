using System;
using System.IO;
using System.Text.Json;
using TibiaSmartScreen.Models;

namespace TibiaSmartScreen.Services;

/// <summary>
/// Handles persistent storage of layouts and application settings as JSON files.
/// Files are stored in %APPDATA%\TibiaSmartScreen\.
/// </summary>
public class LayoutService
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TibiaSmartScreen");

    private static readonly string LayoutsDir = Path.Combine(AppDataDir, "Layouts");
    private static readonly string SettingsPath = Path.Combine(AppDataDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public LayoutService()
    {
        Directory.CreateDirectory(LayoutsDir);
    }

    public void SaveLayout(AppLayout layout)
    {
        layout.LastModified = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(layout, JsonOptions);
        File.WriteAllText(GetLayoutPath(layout.Name), json);
    }

    public AppLayout? LoadLayout(string name)
    {
        string path = GetLayoutPath(name);
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<AppLayout>(File.ReadAllText(path));
        }
        catch { return null; }
    }

    public AppLayout LoadOrCreate(string name)
        => LoadLayout(name) ?? new AppLayout { Name = name };

    public void SaveSettings(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(SettingsPath)) return new AppSettings();
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath))
                   ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    private static string GetLayoutPath(string name)
        => Path.Combine(LayoutsDir, $"{SanitizeName(name)}.json");

    private static string SanitizeName(string name)
        => string.Concat(name.Split(Path.GetInvalidFileNameChars()));
}
