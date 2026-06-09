using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Readscreen.Core.Interfaces;
using Readscreen.Core.Models;

namespace Readscreen.App;

public sealed class AppSettingsService : IAppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;
    private AppSettings _current;

    public AppSettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Readscreen");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");

        if (File.Exists(_settingsPath))
        {
            var json = File.ReadAllText(_settingsPath);
            _current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        else
        {
            _current = new AppSettings { DataDirectory = appData };
            Save();
        }

        if (string.IsNullOrWhiteSpace(_current.DataDirectory))
            _current.DataDirectory = appData;
    }

    public AppSettings Current => _current;

    public event Action? SettingsChanged;

    public void Save()
    {
        var json = JsonSerializer.Serialize(_current, JsonOptions);
        File.WriteAllText(_settingsPath, json);
        SettingsChanged?.Invoke();
    }

    public void Reload()
    {
        if (!File.Exists(_settingsPath))
            return;

        var json = File.ReadAllText(_settingsPath);
        _current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        SettingsChanged?.Invoke();
    }
}
