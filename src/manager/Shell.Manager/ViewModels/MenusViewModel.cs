#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shell.Manager.Services;

namespace Shell.Manager.ViewModels;

public sealed partial class MenusViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _gotoEnabled;

    [ObservableProperty]
    private bool _terminalEnabled;

    [ObservableProperty]
    private bool _developEnabled;

    [ObservableProperty]
    private bool _fileManageEnabled;

    [ObservableProperty]
    private bool _iconsEnabled;

    [ObservableProperty]
    private int _iconSize = 16;

    [ObservableProperty]
    private int _showDelay = 150;

    [ObservableProperty]
    private bool _tipsEnabled;

    public Action<string, int>? Notify { get; set; }

    public void Load()
    {
        GotoEnabled = ShellService.GetSectionEnabled("goto");
        TerminalEnabled = ShellService.GetSectionEnabled("terminal");
        DevelopEnabled = ShellService.GetSectionEnabled("develop");
        FileManageEnabled = ShellService.GetSectionEnabled("file-manage");
        IconsEnabled = ShellService.GetIconsEnabled();
        IconSize = ShellService.GetIconSize();
        ShowDelay = ShellService.GetShowDelay();
        TipsEnabled = ShellService.GetTipsEnabled();
    }

    [RelayCommand]
    private void ToggleSection(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return;
        var on = id switch
        {
            "goto" => GotoEnabled,
            "terminal" => TerminalEnabled,
            "develop" => DevelopEnabled,
            "file-manage" => FileManageEnabled,
            _ => (bool?)null,
        };
        if (on is null)
            return;
        ShellService.SetSectionEnabled(id, on.Value);
        Notify?.Invoke("Restart Explorer to apply.", 2);
    }

    [RelayCommand]
    private void ApplyIcons()
    {
        ShellService.SetIconsEnabled(IconsEnabled);
        Notify?.Invoke("Restart Explorer to apply.", 2);
    }

    [RelayCommand]
    private void ApplyIconSize()
    {
        ShellService.SetIconSize(IconSize);
        Notify?.Invoke("Restart Explorer to apply.", 2);
    }

    [RelayCommand]
    private void ApplyDelay()
    {
        ShellService.SetShowDelay(ShowDelay);
        Notify?.Invoke("Restart Explorer to apply.", 2);
    }

    [RelayCommand]
    private void ApplyTips()
    {
        ShellService.SetTipsEnabled(TipsEnabled);
        Notify?.Invoke("Restart Explorer to apply.", 2);
    }

    [RelayCommand]
    private void EditConfig() => ShellService.EditConfig();
}
