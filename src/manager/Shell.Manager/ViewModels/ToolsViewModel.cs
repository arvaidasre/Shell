#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shell.Manager.Services;

namespace Shell.Manager.ViewModels;

public sealed partial class ToolsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _reportText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private string _latestVersion = string.Empty;

    public Action<string, int>? Notify { get; set; }

    public void Load() { }

    [RelayCommand]
    private void OpenConfigFolder() => ShellService.OpenConfigFolder();

    [RelayCommand]
    private void BackupConfig()
    {
        var path = ShellService.BackupConfig();
        Notify?.Invoke(path is null ? "Backup failed." : $"Backup saved to {path}", path is null ? 3 : 1);
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        IsBusy = true;
        try
        {
            var (current, latest, isNew) = await ShellService.CheckForUpdatesAsync();
            LatestVersion = latest;
            HasUpdate = isNew;
            Notify?.Invoke(
                string.IsNullOrEmpty(latest)
                    ? "Could not check for updates."
                    : isNew ? $"Update available: {latest} (current {current})." : $"Up to date ({current}).",
                string.IsNullOrEmpty(latest) ? 2 : isNew ? 1 : 0);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RunCheck() => ReportText = ShellService.CheckReport();

    [RelayCommand]
    private void OpenReleases() => ShellService.OpenReleasesPage();

    [RelayCommand]
    private void OpenIssues() => ShellService.OpenIssuesPage();

    [RelayCommand]
    private void EnsureShortcut()
    {
        var ok = ShellService.EnsureStartMenuShortcut();
        Notify?.Invoke(ok ? "Start menu shortcut ready." : "Failed to create shortcut.", ok ? 1 : 3);
    }
}
