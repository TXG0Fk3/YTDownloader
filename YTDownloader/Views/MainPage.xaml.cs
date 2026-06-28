using Microsoft.UI.Xaml.Controls;
using YTDownloader.Services;
using YTDownloader.ViewModels;

namespace YTDownloader.Views;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; set; }

    public MainPage()
    {
        InitializeComponent();

        ViewModel = App.GetService<MainPageViewModel>();
        DataContext = ViewModel;
        Loaded += (_, __) => App.GetService<DialogService>().Initialize(XamlRoot);
    }
}
