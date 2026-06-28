using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using YTDownloader.Enums;
using YTDownloader.Helpers;
using YTDownloader.Services;

namespace YTDownloader.ViewModels.Dialogs;

public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly DialogService _dialogService;
    private readonly IMessenger _messenger;

    public string YTDownloaderVersion { get; } = AppInfoHelper.Version;

    public IReadOnlyList<ThemeMode> ThemeOptions { get; } =
        new List<ThemeMode>() { ThemeMode.Light, ThemeMode.Dark, ThemeMode.System };

    [ObservableProperty]
    public partial ThemeMode SelectedThemeOption { get; set; }

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
    private async Task OnSelectTheme() => await SaveSettings();

    [RelayCommand]
    private async Task OnSelectDefaultDownloadsFolder()
    {
        var path = await _dialogService.OpenFolderPickerAsync();
        if (!string.IsNullOrEmpty(path))
        {
            DefaultDownloadsPath = path;
            await SaveSettings();
        }
    }

    [RelayCommand]
    private async Task OnAlwaysAskWhereSave() => await SaveSettings();

    private async Task SaveSettings()
    {
        _settingsService.Set(s => s.Theme, SelectedThemeOption);
        _settingsService.Set(s => s.DefaultDownloadsPath, DefaultDownloadsPath);
        _settingsService.Set(s => s.AlwaysAskWhereSave, IsAlwaysAskWhereSaveOn);

        await _settingsService.SaveAsync();
    }
}
