using Microsoft.UI.Xaml;
using Shell.Manager.Services;
using Shell.Manager.Views;

namespace Shell.Manager;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    public static NavigationService? Navigation { get; private set; }
    public static ThemeService Theme { get; private set; } = new ThemeService();

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        Navigation = new NavigationService(MainWindow.ContentFrame);
        Theme.ApplyTheme(Theme.LoadTheme());
        Navigation.Navigate(typeof(HomePage));
        MainWindow.Activate();

        _ = Task.Run(ShellService.EnsureStartMenuShortcut);
    }
}
