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

    public string Id { get; set; }
    public string Url { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string ThumbnailUrl { get; set; }
    public DownloadType Type { get; set; }
    public string Quality { get; set; }
    public StreamManifest Manifest { get; set; }
    public StreamOption VideoStreamOption { get; set; }
    public StreamOption AudioStreamOption { get; set; }
    public string OutputPath { get; set; }

    [ObservableProperty]
    public partial double Progress { get; set; } = 0.0;

    [ObservableProperty]
    public partial DownloadStatus Status { get; set; } = DownloadStatus.Pending;

    [ObservableProperty]
    public partial Exception? Error { get; set; }

    public IProgress<double> ProgressReporter { get; }
    public CancellationTokenSource CTS { get; set; } = new();

    public DownloadItem() => ProgressReporter = new Progress<double>(p => UpdateProgress(p));

    public double FileSizeMB =>
        Type == DownloadType.Video
            ? VideoStreamOption.SizeMB + AudioStreamOption.SizeMB
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
