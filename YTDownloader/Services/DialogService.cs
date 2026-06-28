using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using YTDownloader.Views.Dialogs;

namespace YTDownloader.Services;

public class DialogService
{
    private XamlRoot? _xamlRoot;

    private ElementTheme AppTheme =>
        _xamlRoot?.Content is FrameworkElement fe ? fe.RequestedTheme : ElementTheme.Default;

    public void Initialize(XamlRoot root) => _xamlRoot = root;

    public async Task ShowDetailsDialogAsync() => await ShowDialogAsync(new DetailsDialog());

    public async Task ShowHelpDialogAsync() => await ShowDialogAsync(new HelpDialog());

    public async Task ShowSettingsDialogAsync() => await ShowDialogAsync(new SettingsDialog());

    public async Task ShowErrorDialogAsync(string message) =>
        await ShowDialogAsync(new ErrorDialog(message));

    public async Task<string?> OpenFolderPickerAsync()
    {
        ThrowIfXamlRootNotInitialized();

        var folderPicker = new FolderPicker(_xamlRoot!.ContentIslandEnvironment.AppWindowId)
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
        };

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task ShowDialogAsync(ContentDialog dialog)
    {
        ThrowIfXamlRootNotInitialized();

        dialog.XamlRoot = _xamlRoot;
        dialog.RequestedTheme = AppTheme;
        await dialog.ShowAsync();
    }

    private void ThrowIfXamlRootNotInitialized()
    {
        if (_xamlRoot == null)
            throw new InvalidOperationException("XamlRoot must be initialized.");
    }
}
