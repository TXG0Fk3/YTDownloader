using System.Collections.Generic;
using YoutubeExplode.Videos.Streams;

namespace YTDownloader.Models.Info;

public class VideoInfo
{
    public string Id { get; init; }
    public string Url { get; init; }
    public string Title { get; init; }
    public string Author { get; init; }
    public string ThumbnailUrl { get; init; }

    public IReadOnlyList<StreamOption> Streams { get; init; }

    internal StreamManifest Manifest { get; init; }
}
