using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace YTDownloader.Helpers;

public static class FileHelper
{
    public static void OpenFolder(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var directory = Path.GetDirectoryName(filePath);
        if (Directory.Exists(directory))
        {
            Process.Start("explorer.exe", directory);
        }
    }

    public static void CreateFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
    }

    public static void DeleteFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"Failed to delete file (in use): {filePath}. Error: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"No permission to delete: {filePath}. Error: {ex.Message}");
        }
    }

    public static void DeleteFolder(string folderPath)
    {
        if (Directory.Exists(folderPath))
            Directory.Delete(folderPath, true);
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "unnamed_file";

        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalidChars.Contains(c)));
    }
}
