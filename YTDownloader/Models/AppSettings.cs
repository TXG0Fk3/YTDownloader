using YTDownloader.Enums;

namespace YTDownloader.Models;

public record AppSettings(ThemeOption Theme, string DefaultDownloadsPath, bool AlwaysAskWhereSave);
