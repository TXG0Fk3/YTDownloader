using System;
using System.IO;

namespace YTDownloader.Helpers;

public static class FFmpegLocator
{
    private static string? _cachedPath;

    public static string GetFFmpegPath()
    {
        if (!string.IsNullOrEmpty(_cachedPath))
            return _cachedPath;

        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(localPath))
            return _cachedPath = localPath;

        if (ExistsInPath("ffmpeg.exe"))
            return _cachedPath = "ffmpeg";

        throw new FileNotFoundException(
            "FFmpeg not found. Make sure it's in the app folder or in the system PATH."
        );
    }

    private static bool ExistsInPath(string fileName)
    {
        var values = Environment.GetEnvironmentVariable("PATH");
        if (values == null)
            return false;

        foreach (var path in values.Split(Path.PathSeparator))
        {
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
                return true;
        }
        return false;
    }
}
