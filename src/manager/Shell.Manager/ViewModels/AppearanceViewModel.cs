#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shell.Manager.Services;

namespace Shell.Manager.ViewModels;

public sealed record LanguageOption(string Code, string Name);

public sealed partial class AppearanceViewModel : ObservableObject
{
    // Codes mapped from existing lang/*.nss files (de-DE.nss -> de, es-ES.nss -> es).
    public List<LanguageOption> Languages { get; } = new()
    {
        new("en", "English"),
        new("lt", "Lietuvių"),
        new("ru", "Русский"),
        new("de", "Deutsch"),
        new("es", "Español"),
    };

    [ObservableProperty]
    private string _selectedLanguageCode = "en";

    [ObservableProperty]
    private LanguageOption _selectedLanguage = new("en", "English");

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
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
        var match = Languages.Find(l => l.Code == code) ?? Languages[0];
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
