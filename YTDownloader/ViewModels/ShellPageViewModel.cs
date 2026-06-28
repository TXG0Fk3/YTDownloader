using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using YTDownloader.Enums;
using YTDownloader.Messages;
using YTDownloader.Models;
using YTDownloader.Services;
using YTDownloader.ViewModels.Components;

namespace YTDownloader.ViewModels;

public partial class ShellPageViewModel
    : ObservableObject,
        IRecipient<DownloadRequestMessage>,
        IRecipient<RetryDownloadRequestMessage>,
        IRecipient<RemoveDownloadRequestMessage>,
        IRecipient<ErrorDialogRequestMessage>
{
    private readonly DownloadsService _downloadsService;
    private readonly SettingsService _settingsService;
    private readonly DialogService _dialogService;
    private readonly IMessenger _messenger;

    private bool _isInitializing = true;

    public ObservableCollection<IDownloadableViewModel> Downloads { get; private set; } = new();

    [ObservableProperty]
    public partial SortDirection SortDirection { get; set; }

    public bool IsDownloadItemsEmpty => Downloads.Count == 0;

    public ShellPageViewModel(
        DownloadsService downloadsService,
        SettingsService settingsService,
        DialogService dialogService,
        IMessenger messenger
    )
    {
        _downloadsService = downloadsService;
        _settingsService = settingsService;
        _dialogService = dialogService;
        _messenger = messenger;

        _messenger.RegisterAll(this);

        SortDirection = _settingsService.Current.DownloadsSortDirection;
        Downloads.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsDownloadItemsEmpty));
        _isInitializing = false;
    }

    public void Receive(DownloadRequestMessage message) => EnqueueDownload(message.DownloadInfo);

    public void Receive(RetryDownloadRequestMessage message) => RetryDownload(message.Item);

    public void Receive(RemoveDownloadRequestMessage message) =>
        RemoveDownload(message.DownloadableViewModel);

    public void Receive(ErrorDialogRequestMessage message) => _ = OpenError(message.ErrorMessage);

    [RelayCommand]
    private void SetSortDirection(string value) => SortDirection = Enum.Parse<SortDirection>(value);

    [RelayCommand]
    private async Task AddDownloadAsync() => await _dialogService.ShowDetailsDialogAsync();

    [RelayCommand]
    private async Task OpenHelp() => await _dialogService.ShowHelpDialogAsync();

    [RelayCommand]
    private async Task OpenSettings() => await _dialogService.ShowSettingsDialogAsync();

    private async Task OpenError(string errorMessage) =>
        await _dialogService.ShowErrorDialogAsync(errorMessage);

    private void EnqueueDownload(IDownloadable downloadable)
    {
        IDownloadableViewModel vm = downloadable switch
        {
            DownloadItem item => new DownloadItemViewModel(item, _messenger),
            DownloadGroup group => new DownloadGroupViewModel(group, _messenger),
            _ => throw new InvalidOperationException("Not supported type"),
        };

        if (SortDirection == SortDirection.Descending)
            Downloads.Insert(0, vm);
        else
            Downloads.Add(vm);

        _ = _downloadsService.EnqueueDownloadable(downloadable);
    }

    private void RetryDownload(DownloadItem item) =>
        _ = _downloadsService.EnqueueDownloadable(item);

    private void RemoveDownload(IDownloadableViewModel vm)
    {
        Downloads.Remove(vm);
        vm.Dispose();
    }

    async partial void OnSortDirectionChanged(SortDirection value)
    {
        if (_isInitializing)
            return;
        _settingsService.Set(s => s.DownloadsSortDirection, value);

        var items = Downloads.ToArray();
        Downloads.Clear();

        foreach (var item in items.Reverse())
            Downloads.Add(item);
    }
}
