#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shell.Manager.Services;

namespace Shell.Manager.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _takeoverText = string.Empty;

    [ObservableProperty]
    private string _configText = string.Empty;

    [ObservableProperty]
    private string _subtitleText = string.Empty;

    [ObservableProperty]
    private bool _isRegistered;

    public Action<string, int>? Notify { get; set; }

    public void Load() => Refresh();

    public void Refresh()
    {
        IsRegistered = ShellService.IsRegistered();
        StatusText = IsRegistered ? "Registered" : "Not registered";
        TakeoverText = ShellService.IsModernTakeover()
            ? "Modern menu takeover: on"
            : "Modern menu takeover: off";
        ConfigText = ShellService.EffectiveConfig();
        SubtitleText = $"Shell {ShellService.ShellVersion}";
    }

    [RelayCommand]
    private void RefreshPage() => Refresh();

    [RelayCommand]
    private void Register()
    {
        var ok = ShellService.RunEngine("-register -treat -restart", true);
        Refresh();
        Notify?.Invoke(ok ? "Registered successfully." : "Registration failed.", ok ? 1 : 3);
    }

    [RelayCommand]
    private void Unregister()
    {
        var ok = ShellService.RunEngine("-unregister", true);
        Refresh();
        Notify?.Invoke(ok ? "Unregistered." : "Unregister failed.", ok ? 1 : 3);
    }

    [RelayCommand]
    private void RestartExplorer()
    {
        var ok = ShellService.RunEngine("-restart", false);
        Notify?.Invoke(ok ? "Explorer restarted." : "Failed to restart Explorer.", ok ? 1 : 3);
    }
}
