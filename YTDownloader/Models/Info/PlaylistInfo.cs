using System.Collections.Generic;

namespace YTDownloader.Models.Info;

public class PlaylistInfo
{
    public string Id { get; init; }
    public string Url { get; init; }
    public string Title { get; init; }
    public string Author { get; init; }
    public string ThumbnailUrl { get; init; }

    public IReadOnlySet<string> Qualities { get; } =
        new HashSet<string>
        {
            "2160p60",
            "2160p",
            "1440p60",
            "1440p",
            "1080p60",
            "1080p",
            "720p60",
            "720p",
            "480p60",
            "480p",
            "360p60",
            "360p",
            "240p60",
            "240p",
            "144p60",
            "144p",
        };
}
