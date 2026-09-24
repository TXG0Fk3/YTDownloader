using System.ComponentModel;
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

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += (_, _) => ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsSettingsOpen) && !ViewModel.IsSettingsOpen)
            Hide();
    }

    private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        RequestedTheme = XamlRoot.Content is FrameworkElement fe
            ? fe.RequestedTheme
            : ElementTheme.Default;
}
