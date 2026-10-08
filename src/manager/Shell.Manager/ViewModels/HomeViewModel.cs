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

    [ObservableProperty]
    private bool _engineFound = true;

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
        EngineFound = ShellService.ShellExe is not null;
        SubtitleText = EngineFound
            ? $"Shell {ShellService.ShellVersion}"
            : "shell.exe was not found next to the Manager. Reinstall Shell or run the Manager from its install folder.";
    }

    [RelayCommand]
    private void RefreshPage() => Refresh();

    [ObservableProperty]
    private bool _isBusy;

    private async Task RunAsync(string args, bool elevated, string okText, string failText)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        try
        {
            // Engine calls can wait on a UAC prompt for a long time; keep the UI responsive.
            var ok = await Task.Run(() => ShellService.RunEngine(args, elevated));
            Refresh();
            Notify?.Invoke(ok ? okText : failText, ok ? 1 : 3);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task Register() =>
        RunAsync("-register -treat -restart", true, "Registered successfully.", "Registration failed.");

    [RelayCommand]
    private Task Unregister() =>
        RunAsync("-unregister -restart", true, "Unregistered.", "Unregister failed.");

    [RelayCommand]
    private Task RestartExplorer() =>
        RunAsync("-restart", false, "Explorer restarted.", "Failed to restart Explorer.");
}
