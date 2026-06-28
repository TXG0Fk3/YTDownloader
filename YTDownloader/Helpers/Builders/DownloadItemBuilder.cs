using System;
using System.IO;
using System.Threading;
using YTDownloader.Enums;
using YTDownloader.Models;
using YTDownloader.Models.Info;

namespace YTDownloader.Helpers.Builders;

public class DownloadItemBuilder
{
    private VideoInfo? _info;
    private string? _outputPath;
    private DownloadType _type;
    private string? _quality;
    private StreamOption? _videoStream;
    private StreamOption? _audioStream;
    private CancellationTokenSource? _cts;

    public DownloadItemBuilder FromVideoInfo(VideoInfo videoInfo)
    {
        if (videoInfo.Manifest == null)
            throw new ArgumentException(
                "The provided VideoInfo has a null StreamManifest.",
                nameof(videoInfo)
            );

        _info = videoInfo;
        return this;
    }

    public DownloadItemBuilder WithOutputPath(string path)
    {
        _outputPath = path;
        return this;
    }

    public DownloadItemBuilder AsVideo(
        string quality,
        StreamOption videoStream,
        StreamOption audioStream
    )
    {
        _type = DownloadType.Video;
        _quality = quality;
        _videoStream = videoStream;
        _audioStream = audioStream;

        return this;
    }

    public DownloadItemBuilder AsAudio(StreamOption audioStream)
    {
        _type = DownloadType.Audio;
        _quality = "Best";
        _audioStream = audioStream;

        return this;
    }

    public DownloadItemBuilder WithGroupCancellation(CancellationToken groupToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(groupToken);
        return this;
    }

    public DownloadItem Build()
    {
        if (_info == null)
            throw new InvalidOperationException(
                "VideoInfo has not been provided. Call FromVideoInfo() before building."
            );

        if (string.IsNullOrWhiteSpace(_outputPath))
            throw new InvalidOperationException(
                "Output path has not been set. Call WithOutputPath() before building."
            );

        if (_type != DownloadType.Video && _type != DownloadType.Audio)
            throw new InvalidOperationException(
                "Download type has not been specified. Call AsVideo() or AsAudio() before building."
            );

        if (string.IsNullOrWhiteSpace(_quality))
            throw new InvalidOperationException(
                "Quality has not been set. Call AsVideo() or AsAudio() before building."
            );

        if (_audioStream == null)
            throw new InvalidOperationException(
                "Audio stream option has not been provided. Call AsVideo() or AsAudio() before building."
            );

        var item = new DownloadItem()
        {
            Id = _info.Id,
            Url = _info.Url,
            Title = _info.Title,
            Author = _info.Author,
            ThumbnailUrl = _info.ThumbnailUrl,
            Type = _type,
            Quality = _quality,
            Manifest = _info.Manifest!,
            VideoStreamOption = _videoStream,
            AudioStreamOption = _audioStream,
            OutputPath = Path.ChangeExtension(
                _outputPath,
                _type == DownloadType.Video ? "mp4" : "mp3"
            ),
        };

        if (_cts != null)
            item.CTS = _cts;

        return item;
    }
}
