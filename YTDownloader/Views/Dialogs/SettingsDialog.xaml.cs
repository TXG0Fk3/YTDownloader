using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YTDownloader.ViewModels.Dialogs;

namespace YTDownloader.Views.Dialogs;

public sealed partial class SettingsDialog : ContentDialog
{
    public SettingsDialogViewModel ViewModel { get; set; }

    public SettingsDialog()
    {
        InitializeComponent();

        ViewModel = App.GetService<SettingsDialogViewModel>();
        DataContext = ViewModel;
    }

    private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        RequestedTheme = XamlRoot.Content is FrameworkElement fe
            ? fe.RequestedTheme
            : ElementTheme.Default;
}
