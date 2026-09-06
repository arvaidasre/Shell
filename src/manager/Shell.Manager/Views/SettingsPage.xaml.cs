using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Shell.Manager.ViewModels;

namespace Shell.Manager.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel.Notify = (m, s) => App.MainWindow?.ShowStatus(m, (InfoBarSeverity)s);
        ViewModel.ApplyThemeRequest = t =>
        {
            App.Theme.SaveTheme(t);
            App.Theme.ApplyTheme(t);
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Load();
    }
}
