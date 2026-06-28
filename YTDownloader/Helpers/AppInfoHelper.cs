using System.Reflection;

namespace YTDownloader.Helpers;

public static class AppInfoHelper
{
    public static string Version { get; } =
        Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "Unknown";
}
