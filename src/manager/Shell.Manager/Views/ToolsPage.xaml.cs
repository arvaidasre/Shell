using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Shell.Manager.ViewModels;

namespace Shell.Manager.Views;

public sealed partial class ToolsPage : Page
{
    public ToolsViewModel ViewModel { get; } = new();

    public ToolsPage()
    {
        InitializeComponent();
        ViewModel.Notify = (m, s) => App.MainWindow?.ShowStatus(m, (InfoBarSeverity)s);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Load();
    }

    private async void RunCheckButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RunCheckCommand.Execute(null);
        ReportTextBlock.Text = ViewModel.ReportText;
        ReportDialog.XamlRoot = XamlRoot;
        await ReportDialog.ShowAsync();
    }
}
