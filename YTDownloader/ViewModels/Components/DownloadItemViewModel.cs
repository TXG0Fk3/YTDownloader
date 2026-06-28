using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using YTDownloader.Enums;
using YTDownloader.Helpers;
using YTDownloader.Messages;
using YTDownloader.Models;

namespace YTDownloader.ViewModels.Components;

public partial class DownloadItemViewModel : ObservableObject, IDownloadableViewModel
{
    private readonly DownloadItem _downloadItem;
    private readonly IMessenger _messenger;

    public string Title => _downloadItem.Title;
    public string Author => _downloadItem.Author;
    public string Url => _downloadItem.Url;
    public string QualityAndSize => $"{_downloadItem.Quality} {_downloadItem.FileSizeMB:F1}MB";
    public string ThumbnailPath => _downloadItem.ThumbnailUrl;
    public Exception? Error => _downloadItem.Error;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsDownloading),
        nameof(IsConverting),
        nameof(IsErrorVisible),
        nameof(FirstButtonIcon),
        nameof(SecondButtonIcon)
    )]
    public partial DownloadStatus Status { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedProgress), nameof(ProgressInfo))]
    public partial double Progress { get; set; }

    public bool IsDownloading => Status == DownloadStatus.Downloading;
    public bool IsConverting => Status == DownloadStatus.Converting;
    public bool IsErrorVisible => Status == DownloadStatus.Error;

    public string FormattedProgress => $"{Progress * 100:00}%";
    public string ProgressInfo
    {
        get
        {
            var t = _downloadItem.RemainingTime;
            var s = _downloadItem.FileSizeMB;

            return $"{_downloadItem.DownloadSpeedMBps:F1}MB/s | "
                + $"~{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2} remaining | "
                + $"{s * Progress:F1}MB / {s:F1}MB";
        }
    }

    public string FirstButtonIcon => Status != DownloadStatus.Error ? "\uE8DA" : "\uE72C";
    public string SecondButtonIcon => Status == DownloadStatus.Completed ? "\uE74D" : "\uF78A";

    public DownloadItemViewModel(DownloadItem downloadItem, IMessenger messenger)
    {
        _downloadItem = downloadItem;
        _messenger = messenger;

        Progress = _downloadItem.Progress;
        Status = _downloadItem.Status;

        _downloadItem.PropertyChanged += OnModelPropertyChanged;
    }

    private bool CanFirstButton() => Status is DownloadStatus.Completed or DownloadStatus.Error;

    [RelayCommand(CanExecute = nameof(CanFirstButton))]
    private void OnFirstButton()
    {
        if (Status == DownloadStatus.Error)
            OnRetry();
        else if (Status == DownloadStatus.Completed)
            OnOpenLocal();
    }

    [RelayCommand]
    private void OnSecondButton()
    {
        if (Status == DownloadStatus.Completed)
            OnDelete();
        else
            OnCancel();
    }

    [RelayCommand]
    private void OnSeeLog() =>
        _messenger.Send(new ErrorDialogRequestMessage(Error?.Message ?? "No Log"));

    private void OnCancel()
    {
        _downloadItem.MarkAsCancelled();
        _messenger.Send(new RemoveDownloadRequestMessage(this, _downloadItem));
    }

    private void OnDelete()
    {
        FileHelper.DeleteFile(_downloadItem.OutputPath);
        _messenger.Send(new RemoveDownloadRequestMessage(this, _downloadItem));
    }

    private void OnOpenLocal() => FileHelper.OpenFolder(_downloadItem.OutputPath);

    private void OnRetry() => _messenger.Send(new RetryDownloadRequestMessage(_downloadItem));

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DownloadItem.Progress):
                Progress = _downloadItem.Progress;
                break;
            case nameof(DownloadItem.Status):
                Status = _downloadItem.Status;
                OnPropertyChanged(nameof(Error));
                FirstButtonCommand.NotifyCanExecuteChanged();
                break;
        }
    }

    public void Dispose()
    {
        _downloadItem.PropertyChanged -= OnModelPropertyChanged;
        _messenger.UnregisterAll(this);
    }
}
