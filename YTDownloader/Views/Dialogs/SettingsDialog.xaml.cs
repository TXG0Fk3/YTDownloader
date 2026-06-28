using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using YTDownloader.Helpers.UI;
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

    private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectThemeCommand.Execute(null);
        RequestedTheme = ThemeHelper.ConvertThemeOptionToElementTheme(
            ViewModel.SelectedThemeOption
        );
    }

    private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e) =>
        ViewModel.AlwaysAskWhereSaveCommand.Execute(null);
}
