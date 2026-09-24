using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using YTDownloader.Enums;
using YTDownloader.Helpers;
using YTDownloader.Messages;
using YTDownloader.Models.Settings;
using YTDownloader.Services;

namespace YTDownloader.ViewModels.Dialogs;

public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly BrowserService _browserService;
    private readonly CredentialService _credentialService;
    private readonly DialogService _dialogService;
    private readonly IMessenger _messenger;

    private bool _isInitializing = true;

    public string YTDownloaderVersion { get; } = AppInfoHelper.Version;

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; } = true;

    public IReadOnlyList<ThemeMode> ThemeOptions { get; } =
        new List<ThemeMode>() { ThemeMode.Light, ThemeMode.Dark, ThemeMode.System };

    [ObservableProperty]
    public partial ThemeMode SelectedThemeMode { get; set; }

    public readonly int MaxConcurrentDownloadsMaximum = 6;
    public readonly int MaxConcurrentPlaylistFetchesMaximum = 16;

    [ObservableProperty]
    public partial int MaxConcurrentPlaylistFetches { get; set; }

    [ObservableProperty]
    public partial int MaxConcurrentDownloads { get; set; }

    [ObservableProperty]
    public partial string? DefaultDownloadsPath { get; set; }

    [ObservableProperty]
    public partial bool AlwaysAskWhereSave { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccountText), nameof(LogInButtonText))]
    public partial bool IsLoggedIn { get; set; }

    public string AccountText => IsLoggedIn ? "Logged-in Account" : "No Account";
    public string LogInButtonText => IsLoggedIn ? "Logout" : "LogIn";

    public SettingsDialogViewModel(
        SettingsService settingsService,
        BrowserService browserService,
        CredentialService credentialService,
        DialogService dialogService,
        IMessenger messenger
    )
    {
        _settingsService = settingsService;
        _browserService = browserService;
        _credentialService = credentialService;
        _dialogService = dialogService;
        _messenger = messenger;

        LoadSettingsAsync();
    }

    private void LoadSettingsAsync()
    {
        SelectedThemeMode = _settingsService.Current.Theme;
        MaxConcurrentPlaylistFetches = _settingsService
            .Current
            .MaxConcurrentPlaylistVideoInfoFetches;
        MaxConcurrentDownloads = _settingsService.Current.MaxConcurrentDownloads;
        DefaultDownloadsPath = _settingsService.Current.DefaultDownloadsPath;
        AlwaysAskWhereSave = _settingsService.Current.AlwaysAskWhereSave;
        IsLoggedIn = _credentialService.HasCookies();

        _isInitializing = false;
    }

    [RelayCommand]
    private async Task LoginOrLogout()
    {
        if (IsLoggedIn)
        {
            _credentialService.ClearCookies();
            IsLoggedIn = false;
            return;
        }

        IsSettingsOpen = false;
        try
        {
            var cookies = await _browserService.ShowYTLoginAndGetCookies();
            _credentialService.SaveCookies(cookies);
            _messenger.Send(new LoginResultMessage(true));
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            _messenger.Send(new LoginResultMessage(false));
        }
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

    partial void OnMaxConcurrentPlaylistFetchesChanged(int value) =>
        ApplySetting(s => s.MaxConcurrentPlaylistVideoInfoFetches, value);

    partial void OnMaxConcurrentDownloadsChanged(int value) =>
        ApplySetting(s => s.MaxConcurrentDownloads, value);

    partial void OnDefaultDownloadsPathChanged(string? value) =>
        ApplySetting(s => s.DefaultDownloadsPath, value);

    partial void OnAlwaysAskWhereSaveChanged(bool value) =>
        ApplySetting(s => s.AlwaysAskWhereSave, value);
}
