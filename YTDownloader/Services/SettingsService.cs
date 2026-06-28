using System;
using System.IO;
using System.Text.Json;
using YTDownloader.Enums;
using YTDownloader.Models;

namespace YTDownloader.Services;

public class SettingsService
{
    private const string FileName = "config.json";
    private readonly string _filePath;

    public AppSettings Current { get; private set; }

    public SettingsService()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "YT Downloader",
            FileName
        );

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        Current = Load();
    }

    private AppSettings Load()
    {
        var defaultSettings = new AppSettings(
            Theme: ThemeOption.System,
            DefaultDownloadsPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            ),
            AlwaysAskWhereSave: true
        );

        if (!File.Exists(_filePath))
            return defaultSettings;

        try
        {
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            if (settings is null)
                return defaultSettings;

            if (
                string.IsNullOrWhiteSpace(settings.DefaultDownloadsPath)
                || !Directory.Exists(settings.DefaultDownloadsPath)
            )
            {
                return settings with
                {
                    DefaultDownloadsPath = defaultSettings.DefaultDownloadsPath,
                };
            }

            return settings;
        }
        catch
        {
            return defaultSettings;
        }
    }

    public void Save(AppSettings settings)
    {
        Current = settings;
        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions { WriteIndented = true }
        );
        File.WriteAllText(_filePath, json);
    }
}
