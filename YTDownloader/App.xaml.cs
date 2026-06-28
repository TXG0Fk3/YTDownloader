using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using YTDownloader.Enums;
using YTDownloader.Helpers.UI;
using YTDownloader.Messages;
using YTDownloader.Services;
using YTDownloader.ViewModels;
using YTDownloader.ViewModels.Dialogs;
using YTDownloader.Views;

namespace YTDownloader;

public partial class App : Application, IRecipient<ChangeThemeRequestMessage>
{
    private static Window? MainWindow;

    private readonly IServiceProvider _services;
    private readonly IMessenger _messenger;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<DetailsDialogViewModel>();
        services.AddTransient<SettingsDialogViewModel>();

        services.AddSingleton<YoutubeService>();
        services.AddSingleton<DownloadsService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<DialogService>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        _services = services.BuildServiceProvider();

        _messenger = GetService<IMessenger>();
        _messenger.RegisterAll(this);
    }

    public static T GetService<T>()
        where T : class => ((App)Current)._services.GetRequiredService<T>();

    public void Receive(ChangeThemeRequestMessage message) => ApplyTheme(message.Theme);

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var rootFrame = new Frame { Content = new SplashPage() };
        MainWindow = new Window
        {
            ExtendsContentIntoTitleBar = true,
            Title = "YT Downloader",
            Content = rootFrame,
        };

        ConfigureWindow(MainWindow);

        await Task.Delay(50);
        MainWindow.Activate();

        await InitializeAppAsync();

        MainWindow.SystemBackdrop = new MicaBackdrop();
        ApplyTheme(GetService<SettingsService>().Current.Theme);

        rootFrame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
    }

    private void ApplyTheme(ThemeOption theme) =>
        ThemeHelper.ApplyTheme(MainWindow!, ThemeHelper.ConvertThemeOptionToElementTheme(theme));

    private void ConfigureWindow(Window window)
    {
        window.AppWindow.SetIcon("Assets/AppIcon.ico");

        var win32Service = new Win32WindowService(window);
        win32Service.SetWindowMinMaxSize(new Win32WindowService.POINT() { x = 430, y = 680 });

        var scaleFactor = win32Service.GetSystemDPI() / 96.0;
        window.AppWindow.Resize(new SizeInt32((int)(430 * scaleFactor), (int)(680 * scaleFactor)));
    }

    private async Task InitializeAppAsync()
    {
        // We keep this method separate for organizational purposes.
        // Today it's purely aesthetic. Tomorrow, if we need to load anything
        // large during app startup, we can simply change it here without altering OnLaunched.
        await Task.Delay(400);
    }
}
