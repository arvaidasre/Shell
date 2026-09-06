#nullable enable
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Shell.Manager.Services;

namespace Shell.Manager.ViewModels;

public sealed class PreviewMenuItem
{
    public string Label { get; init; } = string.Empty;
    public string Glyph { get; init; } = string.Empty;
    public bool HasSubmenu { get; init; }
    public bool IsSeparator { get; init; }
    public bool IsEnabled { get; init; } = true;

    public double ItemOpacity => IsEnabled ? 1.0 : 0.5;

    public Visibility ItemVisibility => IsSeparator ? Visibility.Collapsed : Visibility.Visible;
    public Visibility SeparatorVisibility => IsSeparator ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ChevronVisibility =>
        !IsSeparator && HasSubmenu ? Visibility.Visible : Visibility.Collapsed;

    public static PreviewMenuItem Separator() => new() { IsSeparator = true };
}

public sealed partial class PreviewViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedPreset = "Desktop";

    [ObservableProperty]
    private string _statusesText = string.Empty;

    public ObservableCollection<PreviewMenuItem> MenuItems { get; } = new();

    public Action<string, int>? Notify { get; set; }

    public void Load()
    {
        MenuItems.Clear();

        var enabled = 0;
        foreach (var (id, _) in ShellService.MenuSections)
        {
            var on = ShellService.GetSectionEnabled(id);
            if (on)
                enabled++;
            MenuItems.Add(new PreviewMenuItem
            {
                Label = SectionLabel(id),
                Glyph = SectionGlyph(id),
                HasSubmenu = id is "terminal" or "goto",
                IsEnabled = on,
            });
        }
        StatusesText = $"{enabled} of {ShellService.MenuSections.Length} sections enabled (mock preview).";

        MenuItems.Add(PreviewMenuItem.Separator());
        MenuItems.Add(new() { Label = "Rodyti", Glyph = "", HasSubmenu = true });
        MenuItems.Add(new() { Label = "Rikiuoti", Glyph = "", HasSubmenu = true });
        MenuItems.Add(new() { Label = "Atnaujinti", Glyph = "" });
        MenuItems.Add(PreviewMenuItem.Separator());
        MenuItems.Add(new() { Label = "Įklijuoti", Glyph = "" });
        MenuItems.Add(PreviewMenuItem.Separator());
        MenuItems.Add(new() { Label = "Naujas", Glyph = "", HasSubmenu = true });
        MenuItems.Add(PreviewMenuItem.Separator());
        MenuItems.Add(new() { Label = "Ekrano parametrai", Glyph = "" });
        MenuItems.Add(new() { Label = "Personalizuoti", Glyph = "" });
    }

    partial void OnSelectedPresetChanged(string value) => Load();

    [RelayCommand]
    private void Refresh() => Load();

    private static string SectionLabel(string id) => id switch
    {
        "goto" => "Eiti į",
        "terminal" => "Terminalas",
        "develop" => "Kūrimo įrankiai",
        "file-manage" => "Failų valdymas",
        _ => id,
    };

    private static string SectionGlyph(string id) => id switch
    {
        "goto" => "",
        "terminal" => "",
        "develop" => "",
        "file-manage" => "",
        _ => "",
    };
}
