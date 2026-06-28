using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using YTDownloader.Enums;
using YTDownloader.Helpers;
using YTDownloader.Messages;
using YTDownloader.Models;

namespace YTDownloader.ViewModels.Components;

public partial class DownloadGroupViewModel
    : ObservableObject,
        IDownloadableViewModel,
        IRecipient<RemoveDownloadRequestMessage>
{
    private readonly DownloadGroup _downloadGroup;
    private readonly IMessenger _messenger;

    public string Title => _downloadGroup.Title;
    public string Author => _downloadGroup.Author;
    public string Url => _downloadGroup.Url;
    public string Quality => _downloadGroup.Quality;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedProgress))]
    public partial double Progress { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SecondButtonIcon))]
    public partial DownloadStatus Status { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoadingCardVisible), nameof(IsErrorVisible))]
    public partial Exception? Error { get; set; }

    public string FormattedProgress => $"{Progress * 100:00}%";
    public string SecondButtonIcon => Status == DownloadStatus.Completed ? "\uE74D" : "\uF78A";

    public bool IsLoadingCardVisible => Items.Count == 0 && !IsErrorVisible;
    public bool IsErrorVisible => Error != null;

    public ObservableCollection<DownloadItemViewModel> Items { get; } = new();

    public DownloadGroupViewModel(DownloadGroup downloadGroup, IMessenger messenger)
    {
        _downloadGroup = downloadGroup;
        _messenger = messenger;

        _messenger.RegisterAll(this);

        Progress = _downloadGroup.Progress;
        Status = _downloadGroup.Status;
        Error = _downloadGroup.Error;

        _downloadGroup.PropertyChanged += OnGroupPropertyChanged;
        _downloadGroup.Items.CollectionChanged += OnItemsChanged;
    }

    public void Receive(RemoveDownloadRequestMessage message)
    {
        if (message.DownloadableViewModel is DownloadItemViewModel itemVM)
            OnRemoveItemRequested(itemVM, message.Downloadable as DownloadItem);
    }

    [RelayCommand]
    private void OnOpenLocal() => FileHelper.OpenFolder(_downloadGroup.OutputPath);

    [RelayCommand]
    private void OnDelete()
    {
        _downloadGroup.CTS.Cancel();

        FileHelper.DeleteFolder(_downloadGroup.OutputPath);
        _messenger.Send(new RemoveDownloadRequestMessage(this));
    }

    [RelayCommand]
    private void OnSeeLog() =>
        _messenger.Send(new ErrorDialogRequestMessage(Error?.Message ?? "No Log"));

    private void OnRemoveItemRequested(DownloadItemViewModel itemViewModel, DownloadItem? item)
    {
        if (!Items.Remove(itemViewModel))
            return;
        itemViewModel.Dispose();

        if (item != null)
            _downloadGroup.Items.Remove(item);

        if (Items.Count == 0)
            OnDelete();

        OnPropertyChanged(nameof(IsLoadingCardVisible));
    }

    private void OnGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DownloadGroup.Progress):
                Progress = _downloadGroup.Progress;
                break;
            case nameof(DownloadGroup.Status):
                Status = _downloadGroup.Status;
                break;
            case nameof(DownloadGroup.Error):
                Error = _downloadGroup.Error;
                break;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (DownloadItem item in e.NewItems)
                Items.Add(new DownloadItemViewModel(item, _messenger));

        OnPropertyChanged(nameof(IsLoadingCardVisible));
    }

    public void Dispose()
    {
        _downloadGroup.PropertyChanged -= OnGroupPropertyChanged;
        _downloadGroup.Items.CollectionChanged -= OnItemsChanged;
        _messenger.UnregisterAll(this);

        foreach (var item in Items)
            item.Dispose();
    }
}
