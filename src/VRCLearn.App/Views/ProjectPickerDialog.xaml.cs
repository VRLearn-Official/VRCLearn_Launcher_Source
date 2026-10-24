using System.Windows;
using System.Windows.Controls;
using VRCLearn.App.Services;
using VRCLearn.App.ViewModels;

namespace VRCLearn.App.Views;

public partial class ProjectPickerDialog : DialogWindowBase
{
    public ProjectPickerDialog(
        IReadOnlyList<ProjectChoice> projects,
        int resourceCount,
        LocalizationService localization)
    {
        InitializeComponent();

        CountText.Text = localization.Format("Resources.Picker.Count", resourceCount);
        ProjectList.ItemsSource = projects;
        ProjectList.SelectedItem = projects.FirstOrDefault(x => x.IsSelectable);
    }

    public string? SelectedPath { get; private set; }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ImportButton.IsEnabled = ProjectList.SelectedItem is ProjectChoice { IsSelectable: true };

    private void OnImportClick(object sender, RoutedEventArgs e)
    {
        if (ProjectList.SelectedItem is not ProjectChoice { IsSelectable: true } choice)
        {
            return;
        }

        SelectedPath = choice.Path;
        DialogResult = true;
    }
}
