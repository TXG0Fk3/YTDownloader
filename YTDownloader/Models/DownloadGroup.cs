using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using YTDownloader.Enums;

namespace YTDownloader.Models;

public partial class DownloadGroup : ObservableObject, IDownloadable
{
    public string Id { get; set; }
    public string Url { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public DownloadType Type { get; set; }
    public string Quality { get; set; }
    public string OutputPath { get; set; }
    public ObservableCollection<DownloadItem> Items { get; } = new();
    public CancellationTokenSource CTS { get; private set; } = new();

    [ObservableProperty]
    public partial double Progress { get; set; } = 0.0;

    [ObservableProperty]
    public partial DownloadStatus Status { get; set; } = DownloadStatus.Pending;

    [ObservableProperty]
    public partial Exception? Error { get; set; }

    public DownloadGroup() => Items.CollectionChanged += OnItemsChanged;

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (DownloadItem item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;

        if (e.OldItems != null)
            foreach (DownloadItem item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DownloadItem.Progress))
            Progress = Items.Count == 0 ? 0 : Items.Average(i => i.Progress);

        if (e.PropertyName == nameof(DownloadItem.Status))
            UpdateGroupStatus();
    }

    private void UpdateGroupStatus()
    {
        var statuses = Items.Select(i => i.Status);

        if (statuses.All(s => s == DownloadStatus.Completed))
            Status = DownloadStatus.Completed;
        else if (statuses.Any(s => s is DownloadStatus.Downloading or DownloadStatus.Converting))
            Status = DownloadStatus.Downloading;
        else if (statuses.Any(s => s == DownloadStatus.Error))
            Status = DownloadStatus.Error;
        else
            Status = DownloadStatus.Pending;
    }
}
