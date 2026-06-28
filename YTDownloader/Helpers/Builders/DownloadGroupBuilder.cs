using System;
using YTDownloader.Enums;
using YTDownloader.Models;
using YTDownloader.Models.Info;

namespace YTDownloader.Helpers.Builders;

public class DownloadGroupBuilder
{
    private PlaylistInfo? _info;
    private string? _outputPath;
    private DownloadType _type;
    private string? _quality;

    public DownloadGroupBuilder FromPlaylistInfo(PlaylistInfo playlistInfo)
    {
        _info = playlistInfo;
        return this;
    }

    public DownloadGroupBuilder WithOutputPath(string outputPath)
    {
        _outputPath = outputPath;
        return this;
    }

    public DownloadGroupBuilder WithFormat(DownloadType type, string quality)
    {
        _type = type;
        _quality = quality;
        return this;
    }

    public DownloadGroup Build()
    {
        if (_info == null)
            throw new InvalidOperationException(
                "PlaylistInfo has not been provided. Call FromPlaylistInfo() before building."
            );

        if (string.IsNullOrWhiteSpace(_outputPath))
            throw new InvalidOperationException(
                "Output path has not been set. Call WithOutputPath() before building."
            );

        if (_type != DownloadType.Video && _type != DownloadType.Audio)
            throw new InvalidOperationException(
                "Download type has not been specified. Call WithFormat() before building."
            );

        if (string.IsNullOrWhiteSpace(_quality))
            throw new InvalidOperationException(
                "Quality has not been set. Call WithFormat() before building."
            );

        return new DownloadGroup()
        {
            Id = _info.Id,
            Url = _info.Url,
            Title = _info.Title,
            Author = _info.Author,
            Type = _type,
            Quality = _quality,
            OutputPath = _outputPath,
        };
    }
}
