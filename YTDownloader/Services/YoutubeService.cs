using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using YTDownloader.Enums;
using YTDownloader.Models.Info;

namespace YTDownloader.Services;

public class YoutubeService
{
    private readonly YoutubeClient _youtubeClient = new();
    private readonly SettingsService _settingsService;

    public YoutubeService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<VideoInfo> GetVideoAsync(string videoUrl, CancellationToken ct)
    {
        var video = await _youtubeClient.Videos.GetAsync(videoUrl, ct);
        var streamManifest = await GetStreamManifestAsync(video.Id, ct);

        var streamOptions = streamManifest
            .GetVideoOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .Select(s => new StreamOption
            {
                Quality = s.VideoQuality.Label,
                Format = MediaFormat.Mp4,
                SizeMB = s.Size.MegaBytes,
            })
            .ToList();

        var bestAudio = GetBestAudioOnlyMp4StreamInfo(streamManifest);
        if (bestAudio != null)
        {
            streamOptions.Add(
                new StreamOption
                {
                    Quality = "Best",
                    Format = MediaFormat.Mp3,
                    SizeMB = bestAudio.Size.MegaBytes,
                }
            );
        }

        return new VideoInfo
        {
            Id = video.Id,
            Url = video.Url,
            Title = video.Title,
            Author = video.Author.ToString(),
            ThumbnailUrl = $"https://img.youtube.com/vi/{video.Id}/mqdefault.jpg",
            Streams = streamOptions,
            Manifest = streamManifest,
        };
    }

    public async Task<PlaylistInfo> GetPlaylistAsync(string playlistUrl, CancellationToken ct)
    {
        var playlist = await _youtubeClient.Playlists.GetAsync(playlistUrl, ct);

        return new PlaylistInfo
        {
            Id = playlist.Id,
            Url = playlist.Url,
            Title = playlist.Title,
            Author = playlist.Author?.ToString() ?? "Unknown Author",
        };
    }

    public async Task<IReadOnlyList<VideoInfo>> GetPlaylistVideosAsync(
        string playlistId,
        CancellationToken ct
    )
    {
        using var semaphore = new SemaphoreSlim(
            _settingsService.Current.MaxConcurrentPlaylistVideoInfoFetches
        );

        return await Task.WhenAll(
            (await _youtubeClient.Playlists.GetVideosAsync(playlistId, ct)).Select(async v =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    return await GetVideoAsync(v.Url, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            })
        );
    }

    public StreamOption? GetClosestVideoStreamOption(
        IEnumerable<StreamOption> streams,
        string quality
    )
    {
        var targetResolution = ParseResolution(quality);
        var targetFps = ParseFps(quality);

        return streams
            .Where(s => s.Format == MediaFormat.Mp4)
            .OrderBy(s => Score(s, targetResolution, targetFps))
            .FirstOrDefault();
    }

    public StreamOption? GetBestAudioStreamOption(IEnumerable<StreamOption> streams) =>
        streams.FirstOrDefault(s => s.Format == MediaFormat.Mp3);

    public async Task<(string VideoPath, string AudioPath)> DownloadVideoAsync(
        StreamManifest streamManifest,
        StreamOption videoStreamOption,
        string outputDirectory,
        IProgress<double> progress,
        CancellationToken ct
    )
    {
        var videoStreamInfo =
            GetVideoOnlyStreamInfo(streamManifest, videoStreamOption.Quality)
            ?? throw new InvalidOperationException("Invalid VideoStreamInfo");
        var audioStreamInfo =
            GetBestAudioOnlyMp4StreamInfo(streamManifest)
            ?? throw new InvalidOperationException("Unable to find a compatible MP4 audio stream.");

        string videoPath = Path.Combine(
            outputDirectory,
            $"temp_v_{Guid.NewGuid().ToString().Substring(0, 12)}.{videoStreamInfo.Container.Name}"
        );
        string audioPath = Path.Combine(
            outputDirectory,
            $"temp_a_{Guid.NewGuid().ToString().Substring(0, 12)}.{audioStreamInfo.Container.Name}"
        );

        double videoSize = videoStreamInfo.Size.Bytes;
        double audioSize = audioStreamInfo.Size.Bytes;
        double totalBytes = videoSize + audioSize;

        void ReportProgress(double bytesFromCurrentStream, double bytesAlreadyCompleted)
        {
            double currentTotal = bytesAlreadyCompleted + bytesFromCurrentStream;
            progress.Report(currentTotal / totalBytes);
        }

        var vProgress = new Progress<double>(p => ReportProgress(p * videoSize, 0));
        await _youtubeClient.Videos.Streams.DownloadAsync(
            videoStreamInfo,
            videoPath,
            vProgress,
            ct
        );

        var aProgress = new Progress<double>(p => ReportProgress(p * audioSize, videoSize));
        await _youtubeClient.Videos.Streams.DownloadAsync(
            audioStreamInfo,
            audioPath,
            aProgress,
            ct
        );

        return (videoPath, audioPath);
    }

    public async Task<string> DownloadAudioAsync(
        StreamManifest streamManifest,
        string outputDirectory,
        IProgress<double> progress,
        CancellationToken ct
    )
    {
        var audioStreamInfo =
            GetBestAudioOnlyStreamInfo(streamManifest)
            ?? throw new InvalidOperationException("Invalid AudioStreamInfo");

        string audioPath = Path.Combine(
            outputDirectory,
            $"temp_a_{Guid.NewGuid().ToString().Substring(0, 12)}.{audioStreamInfo.Container.Name}"
        );

        await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, audioPath, progress, ct);
        return audioPath;
    }

    private async Task<StreamManifest> GetStreamManifestAsync(
        string videoId,
        CancellationToken ct
    ) => await _youtubeClient.Videos.Streams.GetManifestAsync(videoId, ct);

    private VideoOnlyStreamInfo? GetVideoOnlyStreamInfo(
        StreamManifest streamManifest,
        string quality
    ) =>
        streamManifest
            .GetVideoOnlyStreams()
            .FirstOrDefault(s => s.Container == Container.Mp4 && s.VideoQuality.Label == quality);

    private AudioOnlyStreamInfo? GetBestAudioOnlyStreamInfo(StreamManifest streamManifest) =>
        streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate() is AudioOnlyStreamInfo audio
            ? audio
            : null;

    private AudioOnlyStreamInfo? GetBestAudioOnlyMp4StreamInfo(StreamManifest streamManifest) =>
        streamManifest
            .GetAudioOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestBitrate()
            is AudioOnlyStreamInfo audio
            ? audio
            : null;

    private static int Score(StreamOption stream, int targetResolution, int targetFps)
    {
        var label = stream.Quality;
        var res = ParseResolution(label);
        var fps = ParseFps(label);
        return Math.Abs(res - targetResolution) + Math.Abs(fps - targetFps);
    }

    private static int ParseResolution(string text) =>
        int.TryParse(text.Split('p')[0], out var res) ? res : 0;

    private static int ParseFps(string text) => text.Split(' ')[0].EndsWith("60") ? 60 : 30;
}
