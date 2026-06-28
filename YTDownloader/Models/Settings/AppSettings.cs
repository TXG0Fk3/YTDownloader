using System;
using System.IO;
using YTDownloader.Enums;

namespace YTDownloader.Models.Settings;

public class AppSettings
{
    public WindowState MainWindowState { get; set; } = new();
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    public string DefaultDownloadsPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public bool AlwaysAskWhereSave { get; set; } = true;
}
