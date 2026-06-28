using Microsoft.UI.Xaml.Controls;

namespace YTDownloader.Views.Dialogs;

public sealed partial class ErrorDialog : ContentDialog
{
    public ErrorDialog(string errorMessage)
    {
        InitializeComponent();
        ErrorTextBlock.Text = errorMessage;
    }
}
