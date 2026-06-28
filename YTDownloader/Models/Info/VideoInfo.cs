using System;
using System.Collections.Generic;
using YoutubeExplode.Videos.Streams;

namespace YTDownloader.Models.Info;

public class VideoInfo
{
    public required string Id { get; set; }
    public required string Url { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string ThumbnailUrl { get; set; }

    public IReadOnlyList<StreamOption> Streams { get; set; } = Array.Empty<StreamOption>();

    internal StreamManifest? Manifest { get; set; }
}
