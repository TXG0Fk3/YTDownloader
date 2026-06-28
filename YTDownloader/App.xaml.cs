using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using YTDOwnloader.Helpers;
using YTDownloader.Services;
using YTDownloader.ViewModels;
using YTDownloader.ViewModels.Dialogs;

namespace YTDownloader;

public partial class App : Application
{
    private const string AppInstanceKey = "YTDownloader.MainApp";
    private static MainWindow? _mainWindow;

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

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var currentInstance = AppInstance.FindOrRegisterForKey(AppInstanceKey);
        if (!currentInstance.IsCurrent)
        {
            await currentInstance.RedirectActivationToAsync(
                AppInstance.GetCurrent().GetActivatedEventArgs()
            );

            Environment.Exit(0);
            return;
        }
        currentInstance.Activated += OnAppInstanceActivated;

        var settings = GetService<SettingsService>();
        await settings.LoadAsync();

        _mainWindow = new MainWindow();

        await Task.Delay(50); // Small delay to avoid black window
        _mainWindow.Activate();

        await Task.Delay(400); // Small delay to ensure the main window is fully ready before navigating
        _mainWindow.NavigateToShell();
    }

    // --- Activation Handling ---

    private void OnAppInstanceActivated(object? sender, AppActivationArguments e)
    {
        _mainWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            WindowHelper.BringToFront(_mainWindow);

            if (e.Kind == ExtendedActivationKind.Protocol)
            {
                var uri = (e.Data as ProtocolActivatedEventArgs)?.Uri;
                // Handle deep links in the future if necessary
            }
        });
    }
}
