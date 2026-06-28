using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YTDownloader.Enums;
using YTDownloader.Helpers;
using YTDownloader.Models.Settings;
using YTDownloader.Services;

namespace YTDownloader.ViewModels.Dialogs;

public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly DialogService _dialogService;

    private bool _isInitializing = true;

    public string YTDownloaderVersion { get; } = AppInfoHelper.Version;

    public IReadOnlyList<ThemeMode> ThemeOptions { get; } =
        new List<ThemeMode>() { ThemeMode.Light, ThemeMode.Dark, ThemeMode.System };

    [ObservableProperty]
    public partial ThemeMode SelectedThemeMode { get; set; }

    [ObservableProperty]
    public partial string? DefaultDownloadsPath { get; set; }

    [ObservableProperty]
    public partial bool AlwaysAskWhereSave { get; set; }

    public SettingsDialogViewModel(SettingsService settingsService, DialogService dialogService)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;

        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        SelectedThemeMode = _settingsService.Current.Theme;
        DefaultDownloadsPath = _settingsService.Current.DefaultDownloadsPath;
        AlwaysAskWhereSave = _settingsService.Current.AlwaysAskWhereSave;

        _isInitializing = false;
    }

    [RelayCommand]
    private async Task SelectDefaultDownloadsFolder()
    {
        var path = await _dialogService.OpenFolderPickerAsync();
        if (!string.IsNullOrEmpty(path))
            DefaultDownloadsPath = path;
    }

    private void ApplySetting<T>(Expression<Func<AppSettings, T>> selector, T value)
    {
        if (_isInitializing)
            return;
        _settingsService.Set(selector, value);
    }

    partial void OnSelectedThemeModeChanged(ThemeMode value) => ApplySetting(s => s.Theme, value);

    partial void OnDefaultDownloadsPathChanged(string? value) =>
        ApplySetting(s => s.DefaultDownloadsPath, value);

    partial void OnAlwaysAskWhereSaveChanged(bool value) =>
        ApplySetting(s => s.AlwaysAskWhereSave, value);
}
