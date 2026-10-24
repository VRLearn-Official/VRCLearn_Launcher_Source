using System.Windows;
using VRCLearn.App.ViewModels;

namespace VRCLearn.App.Views;

public partial class CheckReportDialog : DialogWindowBase
{
    public CheckReportDialog(string title, IEnumerable<CheckReportItem> items, string closeText)
    {
        InitializeComponent();

        Title = title;
        Rows.ItemsSource = items;
        CloseButton.Content = closeText;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
