using System.IO;
using System.Threading;
using YTDownloader.Enums;
using YTDownloader.Models;
using YTDownloader.Models.Info;

namespace YTDownloader.Helpers.Builders;

public class DownloadItemBuilder
{
    private readonly DownloadItem _item = new();

    public DownloadItemBuilder FromVideoInfo(VideoInfo video)
    {
        _item.Id = video.Id;
        _item.Url = video.Url;
        _item.Title = video.Title;
        _item.Author = video.Author;
        _item.ThumbnailUrl = video.ThumbnailUrl;
        _item.Manifest = video.Manifest;
        _item.CTS = new();
        return this;
    }

    public DownloadItemBuilder WithOutputPath(string path)
    {
        _item.OutputPath = path;
        return this;
    }

    public DownloadItemBuilder AsVideo(
        string quality,
        StreamOption videoStream,
        StreamOption audioStream
    )
    {
        _item.Type = DownloadType.Video;
        _item.Quality = quality;
        _item.VideoStreamOption = videoStream;
        _item.AudioStreamOption = audioStream;
        _item.OutputPath = Path.ChangeExtension(_item.OutputPath, "mp4");
        return this;
    }

    public DownloadItemBuilder AsAudio(StreamOption audioStream)
    {
        _item.Type = DownloadType.Audio;
        _item.Quality = "Best";
        _item.AudioStreamOption = audioStream;
        _item.OutputPath = Path.ChangeExtension(_item.OutputPath, "mp3");
        return this;
    }

    public DownloadItemBuilder WithGroupCancellation(CancellationToken groupToken)
    {
        _item.CTS = CancellationTokenSource.CreateLinkedTokenSource(groupToken);
        return this;
    }

    public DownloadItem Build() => _item;
}
