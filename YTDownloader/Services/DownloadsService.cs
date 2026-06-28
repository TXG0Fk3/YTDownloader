using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using YTDownloader.Enums;
using YTDownloader.Helpers;
using YTDownloader.Helpers.Builders;
using YTDownloader.Models;

namespace YTDownloader.Services;

public class DownloadsService
{
    private readonly YoutubeService _youtubeService;
    private readonly SemaphoreSlim _semaphore;

    private readonly Channel<DownloadItem> _downloadQueue;

    public DownloadsService(YoutubeService youtubeService, int maxParallelDownloads = 3)
    {
        _youtubeService = youtubeService;
        _semaphore = new SemaphoreSlim(maxParallelDownloads);

        _downloadQueue = Channel.CreateUnbounded<DownloadItem>();
        _ = ConsumeQueueAsync();
    }

    public async Task EnqueueDownloadable(IDownloadable downloadable)
    {
        if (downloadable is DownloadItem item)
            await _downloadQueue.Writer.WriteAsync(item);
        else if (downloadable is DownloadGroup group)
            _ = EnqueueGroupAsync(group);
    }

    public async Task EnqueueGroupAsync(DownloadGroup group)
    {
        try
        {
            var groupVideoInfos = await _youtubeService.GetPlaylistVideosAsync(
                group.Id,
                group.CTS.Token
            );
            group.CTS.Token.ThrowIfCancellationRequested();

            FileHelper.CreateFolder(group.OutputPath);

            foreach (var videoInfo in groupVideoInfos)
            {
                var builder = new DownloadItemBuilder()
                    .FromVideoInfo(videoInfo)
                    .WithOutputPath(
                        Path.Combine(group.OutputPath, FileHelper.SanitizeFileName(videoInfo.Title))
                    )
                    .WithGroupCancellation(group.CTS.Token);

                var item =
                    group.Type == DownloadType.Video
                        ? builder
                            .AsVideo(
                                group.Quality,
                                _youtubeService.GetClosestMp4StreamOption(
                                    videoInfo.Streams,
                                    group.Quality
                                ),
                                _youtubeService.GetBestMp3StreamOption(videoInfo.Streams)
                            )
                            .Build()
                        : builder
                            .AsAudio(_youtubeService.GetBestMp3StreamOption(videoInfo.Streams))
                            .Build();

                group.Items.Add(item);
                await _downloadQueue.Writer.WriteAsync(item);
            }
        }
        catch (Exception ex)
        {
            group.Error = ex;
        }
    }

    private async Task ConsumeQueueAsync()
    {
        await foreach (var item in _downloadQueue.Reader.ReadAllAsync())
            _ = DownloadAsync(item);
    }

    private async Task DownloadAsync(DownloadItem item)
    {
        await _semaphore.WaitAsync();

        string tempVideo = null;
        string tempAudio = null;

        string tempDirectory = Path.GetTempPath();
        string ffmpegArgs = string.Empty;

        try
        {
            item.CTS.Token.ThrowIfCancellationRequested();
            item.MarkAsDownloading();

            if (item.Type == DownloadType.Video)
            {
                (tempVideo, tempAudio) = await _youtubeService.DownloadVideoAsync(
                    item.Manifest,
                    item.VideoStreamOption,
                    tempDirectory,
                    item.ProgressReporter,
                    item.CTS.Token
                );

                ffmpegArgs =
                    $"-i \"{tempVideo}\" -i \"{tempAudio}\" -c copy -y \"{item.OutputPath}\"";
            }
            else
            {
                tempAudio = await _youtubeService.DownloadAudioAsync(
                    item.Manifest,
                    tempDirectory,
                    item.ProgressReporter,
                    item.CTS.Token
                );

                ffmpegArgs = $"-i \"{tempAudio}\" -vn -ab 192k -y \"{item.OutputPath}\"";
            }

            item.MarkAsConverting();
            await RunFFmpegCommandAsync(ffmpegArgs, item.CTS.Token);

            item.MarkAsCompleted();
        }
        catch (OperationCanceledException)
        {
            item.MarkAsCancelled();
            FileHelper.DeleteFile(item.OutputPath);
        }
        catch (Exception ex)
        {
            item.MarkAsError(ex);
        }
        finally
        {
            FileHelper.DeleteFile(tempVideo);
            FileHelper.DeleteFile(tempAudio);
            _semaphore.Release();
        }
    }

    private async Task RunFFmpegCommandAsync(string arguments, CancellationToken token)
    {
        var ffmpegPath = FFmpegLocator.GetFFmpegPath();

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var errorReadTask = process.StandardError.ReadToEndAsync();

        using var registration = token.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(true);
            }
            catch { }
        });

        await process.WaitForExitAsync(token);
        var fullLog = await errorReadTask;

        if (process.ExitCode != 0)
            throw new Exception($"FFmpeg failed with code {process.ExitCode}: {fullLog}");
    }
}
