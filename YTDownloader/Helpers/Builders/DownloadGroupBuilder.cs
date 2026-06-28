using YTDownloader.Enums;
using YTDownloader.Models;
using YTDownloader.Models.Info;

namespace YTDownloader.Helpers.Builders;

public class DownloadGroupBuilder
{
    private readonly DownloadGroup _group = new();

    public DownloadGroupBuilder FromPlaylistInfo(PlaylistInfo playlist)
    {
        _group.Id = playlist.Id;
        _group.Url = playlist.Url;
        _group.Title = playlist.Title;
        _group.Author = playlist.Author;
        return this;
    }

    public DownloadGroupBuilder WithOutputPath(string outputPath)
    {
        _group.OutputPath = outputPath;
        return this;
    }

    public DownloadGroupBuilder WithFormat(DownloadType type, string quality)
    {
        _group.Type = type;
        _group.Quality = quality;
        return this;
    }

    public DownloadGroup Build() => _group;
}
