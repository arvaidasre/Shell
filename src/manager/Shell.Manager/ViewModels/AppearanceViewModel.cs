#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shell.Manager.Services;

namespace Shell.Manager.ViewModels;

public sealed record LanguageOption(string Code, string Name);

public sealed partial class AppearanceViewModel : ObservableObject
{
    // One entry per imports/lang/<code>.nss file shipped with the installer.
    public List<LanguageOption> Languages { get; } = ShellService.LanguageCodes
        .Select(c => new LanguageOption(c, ShellService.LanguageName(c)))
        .ToList();

    [ObservableProperty]
    private string _selectedLanguageCode = "en";

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value is not null)
            SelectedLanguageCode = value.Code;
    }

    // "default" = imports/theme.nss; rest map to imports/themes/<name>.nss.
    public List<string> ThemePresets { get; } = new() { "default", "mica", "oled", "light", "win11" };

    [ObservableProperty]
    private string _selectedThemePreset = "default";

    public Action<string, int>? Notify { get; set; }

    public void Load()
    {
        var code = ShellService.GetLanguage();
        var match = Languages.Find(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? Languages[0];
        SelectedLanguage = match;
        SelectedLanguageCode = match.Code;
    }

    [RelayCommand]
    private void ApplyLanguage()
    {
        var ok = ShellService.SetLanguage(SelectedLanguageCode);
        Notify?.Invoke(ok ? "Language applied." : "Failed to apply language.", ok ? 1 : 3);
    }

    [RelayCommand]
    private void ApplyTheme()
    {
        var ok = ShellService.ApplyThemePreset(SelectedThemePreset);
        Notify?.Invoke(ok ? "Theme applied." : "Theme preset not found.", ok ? 1 : 3);
    }
}
