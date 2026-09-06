#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shell.Manager.Services;

namespace Shell.Manager.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _appTheme = "System";

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private string _configPath = string.Empty;

    public Action<string, int>? Notify { get; set; }

    /// <summary>Wired by the page code-behind to Services/ThemeService.</summary>
    public Action<string>? ApplyThemeRequest { get; set; }

    public void Load()
    {
        AppVersion = ShellService.ShellVersion;
        ConfigPath = ShellService.EffectiveConfig();
    }

    [RelayCommand]
    private void ApplyAppTheme()
    {
        ApplyThemeRequest?.Invoke(AppTheme);
        Notify?.Invoke("App theme applied.", 1);
    }

    [RelayCommand]
    private void OpenReleases() => ShellService.OpenReleasesPage();
}
