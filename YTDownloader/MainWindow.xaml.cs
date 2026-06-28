using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.UI;
using YTDownloader.Enums;
using YTDownloader.Models.Settings;
using YTDOwnloader.Models.Settings;
using YTDownloader.Services;
using YTDownloader.Views;

namespace YTDownloader;

public sealed partial class MainWindow : Window
{
    private const string WindowTitle = "YT Downloader";
    private const string WindowIconPath = "Assets/AppIcon.ico";

    private const int MinWidth = 430;
    private const int MinHeight = 600;

    private readonly SettingsService _settingsService;

    private readonly double _scaleFactor;
    private readonly OverlappedPresenter _presenter;

    private readonly Frame _rootFrame = new() { Content = new SplashPage() };

    public MainWindow()
    {
        InitializeComponent();
        _settingsService = App.GetService<SettingsService>();

        Title = WindowTitle;
        AppWindow.SetIcon(WindowIconPath);
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        Content = _rootFrame;
        SetTheme(_settingsService.Current.Theme);

        _presenter = OverlappedPresenter.Create();
        _presenter.PreferredMinimumWidth = MinWidth;
        _presenter.PreferredMinimumHeight = MinHeight;
        AppWindow.SetPresenter(_presenter);

        _scaleFactor = GetScaleFactor();
        AppWindow.MoveAndResize(
            new RectInt32(
                (int)(_settingsService.Current.MainWindowState.Left * _scaleFactor),
                (int)(_settingsService.Current.MainWindowState.Top * _scaleFactor),
                (int)(_settingsService.Current.MainWindowState.Width * _scaleFactor),
                (int)(_settingsService.Current.MainWindowState.Height * _scaleFactor)
            )
        );

        if (_settingsService.Current.MainWindowState.IsMaximized)
            _presenter.Maximize();

        _settingsService.SettingChanged += Settings_Changed;
        Closed += MainWindow_Closed;
    }

    public void NavigateToShell()
    {
        SystemBackdrop = new MicaBackdrop();
        _rootFrame.Navigate(typeof(ShellPage), null, new DrillInNavigationTransitionInfo());
    }

    private void SetTheme(ThemeMode themeMode)
    {
        var newTheme = themeMode switch
        {
            ThemeMode.Light => ElementTheme.Light,
            ThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
        _rootFrame.RequestedTheme = newTheme;

        if (newTheme == ElementTheme.Default)
            newTheme =
                Application.Current.RequestedTheme == ApplicationTheme.Dark
                    ? ElementTheme.Dark
                    : ElementTheme.Light;

        Color buttonHoverBackgroundColor =
            newTheme == ElementTheme.Dark ? Color.FromArgb(255, 61, 61, 61) : Colors.LightGray;

        Color foregroundColor = newTheme == ElementTheme.Dark ? Colors.White : Colors.Black;

        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonHoverBackgroundColor = buttonHoverBackgroundColor;
        titleBar.ForegroundColor = foregroundColor;
        titleBar.ButtonForegroundColor = foregroundColor;
        titleBar.ButtonHoverForegroundColor = foregroundColor;
    }

    private void Settings_Changed(object? sender, SettingsChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.Theme))
            SetTheme(e.NewValue is ThemeMode newTheme ? newTheme : ThemeMode.System);
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        args.Handled = true;

        var isMinimized = _presenter.State == OverlappedPresenterState.Minimized;
        if (!isMinimized)
        {
            var isMaximized = _presenter.State == OverlappedPresenterState.Maximized;
            var lastWindowState = new WindowState
            {
                IsMaximized = isMaximized,
                Width = isMaximized
                    ? _settingsService.Current.MainWindowState.Width
                    : AppWindow.Size.Width / _scaleFactor,
                Height = isMaximized
                    ? _settingsService.Current.MainWindowState.Height
                    : AppWindow.Size.Height / _scaleFactor,
                Left = isMaximized
                    ? _settingsService.Current.MainWindowState.Left
                    : AppWindow.Position.X / _scaleFactor,
                Top = isMaximized
                    ? _settingsService.Current.MainWindowState.Top
                    : AppWindow.Position.Y / _scaleFactor,
            };
            _settingsService.Set(s => s.MainWindowState, lastWindowState);
        }

        await _settingsService.SaveAsync();

        _settingsService.SettingChanged -= Settings_Changed;
        Closed -= MainWindow_Closed;
        Close();
    }

    [DllImport("user32.dll")]
    private static extern int GetDpiForWindow(IntPtr hwnd);

    // DPI scaling factor is used to convert logical coordinates to physical pixels
    // for MoveAndResize, which operates in physical pixels on WinUI3.
    private double GetScaleFactor()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        return GetDpiForWindow(hwnd) / 96.0;
    }
}
