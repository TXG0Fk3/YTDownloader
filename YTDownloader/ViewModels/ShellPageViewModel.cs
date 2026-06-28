using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
    private readonly DialogService _dialogService;
    private readonly IMessenger _messenger;

    public ObservableCollection<IDownloadableViewModel> Downloads { get; private set; } = new();

    public bool IsDownloadItemsEmpty => Downloads.Count == 0;

    public ShellPageViewModel(
        DownloadsService downloadsService,
        DialogService dialogService,
        IMessenger messenger
    )
    {
        _downloadsService = downloadsService;
        _dialogService = dialogService;
        _messenger = messenger;

        _messenger.RegisterAll(this);

        Downloads.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsDownloadItemsEmpty));
    }

    public void Receive(DownloadRequestMessage message) => OnEnqueueDownload(message.DownloadInfo);

    public void Receive(RetryDownloadRequestMessage message) => OnRetryDownload(message.Item);

    public void Receive(RemoveDownloadRequestMessage message) =>
        OnRemoveDownload(message.DownloadableViewModel);

    public void Receive(ErrorDialogRequestMessage message) => _ = OnError(message.ErrorMessage);

    [RelayCommand]
    private async Task OnAddDownloadAsync() => await _dialogService.ShowDetailsDialogAsync();

    [RelayCommand]
    private async Task OnHelp() => await _dialogService.ShowHelpDialogAsync();

    [RelayCommand]
    private async Task OnSettings() => await _dialogService.ShowSettingsDialogAsync();

    private async Task OnError(string errorMessage) =>
        await _dialogService.ShowErrorDialogAsync(errorMessage);

    private void OnEnqueueDownload(IDownloadable downloadable)
    {
        if (downloadable is DownloadItem item)
            Downloads.Add(new DownloadItemViewModel(item, _messenger));
        else if (downloadable is DownloadGroup group)
            Downloads.Add(new DownloadGroupViewModel(group, _messenger));

        _ = _downloadsService.EnqueueDownloadable(downloadable);
    }

    private void OnRetryDownload(DownloadItem item) =>
        _ = _downloadsService.EnqueueDownloadable(item);

    private void OnRemoveDownload(IDownloadableViewModel vm)
    {
        Downloads.Remove(vm);
        vm.Dispose();
    }
}
