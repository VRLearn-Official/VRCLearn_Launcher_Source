using System.Windows;
using Wpf.Ui.Controls;

namespace VRCLearn.App.Views;

public partial class ConfirmDialog : DialogWindowBase
{
    public ConfirmDialog(
        string title,
        string message,
        string confirmText,
        string? extraText = null,
        bool confirmDanger = false,
        bool extraDanger = false)
    {
        InitializeComponent();

        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;

        if (confirmDanger)
        {
            ConfirmButton.Appearance = ControlAppearance.Danger;
        }

        if (extraText is null)
        {
            ExtraButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            ExtraButton.Content = extraText;

            if (extraDanger)
            {
                ExtraButton.Appearance = ControlAppearance.Danger;
            }
        }
    }

    public bool ExtraClicked { get; private set; }

    private void OnExtraClick(object sender, RoutedEventArgs e)
    {
        ExtraClicked = true;
        DialogResult = false;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
