using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using YoutubeExplode.Videos.Streams;
using YTDownloader.Enums;
using YTDownloader.Models.Info;

namespace YTDownloader.Models;

public partial class DownloadItem : ObservableObject, IDownloadable
{
    private DateTime? _startTime;

    public required string Id { get; set; }
    public required string Url { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string ThumbnailUrl { get; set; }
    public required DownloadType Type { get; set; }
    public required string Quality { get; set; }
    public required StreamManifest Manifest { get; set; }
    public required StreamOption? VideoStreamOption { get; set; }
    public required StreamOption AudioStreamOption { get; set; }
    public required string OutputPath { get; set; }

    [ObservableProperty]
    public partial double Progress { get; set; } = 0.0;

    [ObservableProperty]
    public partial DownloadStatus Status { get; set; } = DownloadStatus.Pending;

    [ObservableProperty]
    public partial Exception? Error { get; set; }

    public IProgress<double> ProgressReporter { get; }
    public CancellationTokenSource CTS { get; set; } = new();

    internal DownloadItem() => ProgressReporter = new Progress<double>(UpdateProgress);

    public double FileSizeMB =>
        Type == DownloadType.Video
            ? VideoStreamOption!.SizeMB + AudioStreamOption.SizeMB
            : AudioStreamOption.SizeMB;

    public TimeSpan RemainingTime
    {
        get
        {
            if (!_startTime.HasValue || Progress <= 0)
                return TimeSpan.Zero;

            var elapsedTime = DateTime.Now - _startTime.Value;
            var totalSeconds = elapsedTime.TotalSeconds / Progress;
            var remainingSeconds = totalSeconds * (1 - Progress);
            return TimeSpan.FromSeconds(remainingSeconds);
        }
    }

    public double DownloadSpeedMBps
    {
        get
        {
            if (!_startTime.HasValue || Progress <= 0)
                return 0.0;

            var elapsedTime = DateTime.Now - _startTime.Value;
            var downloadedMB = FileSizeMB * Progress;
            return downloadedMB / elapsedTime.TotalSeconds;
        }
    }

    public void UpdateProgress(double value)
    {
        if (Math.Abs(value % 0.01) < 0.0004 || value == 1.0)
            Progress = Math.Clamp(value, 0.0, 1.0);
    }

    public void MarkAsDownloading()
    {
        if (!_startTime.HasValue)
            _startTime = DateTime.Now;
        Status = DownloadStatus.Downloading;
    }

    public void MarkAsConverting()
    {
        Status = DownloadStatus.Converting;
    }

    public void MarkAsCompleted()
    {
        Progress = 1.0;
        Status = DownloadStatus.Completed;
    }

    public void MarkAsCancelled()
    {
        CTS.Cancel();
        Status = DownloadStatus.Cancelled;
    }

    public void MarkAsError(Exception ex)
    {
        Error = ex;
        Status = DownloadStatus.Error;
    }
}
