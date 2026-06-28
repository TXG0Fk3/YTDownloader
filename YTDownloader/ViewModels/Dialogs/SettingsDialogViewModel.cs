using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using YTDownloader.Enums;
using YTDownloader.Helpers;
using YTDownloader.Messages;
using YTDownloader.Models;
using YTDownloader.Services;

namespace YTDownloader.ViewModels.Dialogs;

public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly DialogService _dialogService;
    private readonly IMessenger _messenger;

    public string YTDownloaderVersion { get; } = AppInfoHelper.Version;

    public IReadOnlyList<ThemeOption> ThemeOptions { get; } =
        new List<ThemeOption>() { ThemeOption.Light, ThemeOption.Dark, ThemeOption.System };

    [ObservableProperty]
    public partial ThemeOption SelectedThemeOption { get; set; }

    [ObservableProperty]
    public partial string DefaultDownloadsPath { get; set; }

    [ObservableProperty]
    public partial bool IsAlwaysAskWhereSaveOn { get; set; }

    public SettingsDialogViewModel(
        SettingsService settingsService,
        DialogService dialogService,
        IMessenger messenger
    )
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
        _messenger = messenger;

        SelectedThemeOption = _settingsService.Current.Theme;
        DefaultDownloadsPath = _settingsService.Current.DefaultDownloadsPath;
        IsAlwaysAskWhereSaveOn = _settingsService.Current.AlwaysAskWhereSave;
    }

    [RelayCommand]
    private void OnSelectTheme() => SaveSettings();

    [RelayCommand]
    private async Task OnSelectDefaultDownloadsFolder()
    {
        var path = await _dialogService.OpenFolderPickerAsync();
        if (!string.IsNullOrEmpty(path))
        {
            DefaultDownloadsPath = path;
            SaveSettings();
        }
    }

    [RelayCommand]
    private void OnAlwaysAskWhereSave() => SaveSettings();

    private void SaveSettings()
    {
        var newSettings = new AppSettings(
            Theme: SelectedThemeOption,
            DefaultDownloadsPath: DefaultDownloadsPath,
            AlwaysAskWhereSave: IsAlwaysAskWhereSaveOn
        );

        _settingsService.Save(newSettings);
        _messenger.Send(new ChangeThemeRequestMessage(SelectedThemeOption));
    }
}
