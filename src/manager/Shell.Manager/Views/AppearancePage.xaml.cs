using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Shell.Manager.ViewModels;

namespace Shell.Manager.Views;

public sealed partial class AppearancePage : Page
{
    public AppearanceViewModel ViewModel { get; } = new();

    public AppearancePage()
    {
        InitializeComponent();
        ViewModel.Notify = (m, s) => App.MainWindow?.ShowStatus(m, (InfoBarSeverity)s);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Load();
    }
}
