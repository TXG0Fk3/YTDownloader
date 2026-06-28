using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using YTDownloader.Helpers.UI;
using YTDownloader.Views.Dialogs;

namespace YTDownloader.Services;

public class DialogService
{
    private readonly SettingsService _settingsService;
    private XamlRoot _xamlRoot;

    public DialogService(SettingsService settingsService) => _settingsService = settingsService;

    public void Initialize(XamlRoot root) => _xamlRoot = root;

    public async Task ShowDetailsDialogAsync() => await ShowDialogAsync(new DetailsDialog());

    public async Task ShowHelpDialogAsync() => await ShowDialogAsync(new HelpDialog());

    public async Task ShowSettingsDialogAsync() => await ShowDialogAsync(new SettingsDialog());

    public async Task ShowErrorDialogAsync(string message) =>
        await ShowDialogAsync(new ErrorDialog(message));

    public async Task<string?> OpenFolderPickerAsync()
    {
        var folderPicker = new FolderPicker(_xamlRoot.ContentIslandEnvironment.AppWindowId)
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
        };

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private async Task ShowDialogAsync(ContentDialog dialog)
    {
        if (_xamlRoot != null)
        {
            dialog.XamlRoot = _xamlRoot;
            dialog.RequestedTheme = ThemeHelper.ConvertThemeOptionToElementTheme(
                _settingsService.Current.Theme
            );

            await dialog.ShowAsync();
        }
    }
}
