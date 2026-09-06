using Microsoft.UI.Xaml;

namespace Shell.Manager.Services;

/// <summary>
/// App-level theme (Light / Dark / Default) persisted to a plain text file.
/// File storage is used instead of ApplicationData.LocalSettings because the
/// manager ships unpackaged (no package identity), where ApplicationData throws.
/// Applies via the MainWindow root FrameworkElement RequestedTheme.
/// </summary>
public sealed class ThemeService
{
    private const string FileName = "manager-theme.txt";

    private static string SettingsPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Nilesoft", "Shell");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, FileName);
    }

    public string LoadTheme()
    {
        try
        {
            var path = SettingsPath();
            if (File.Exists(path))
                return Normalize(File.ReadAllText(path).Trim());
        }
        catch { }
        return ElementTheme.Default.ToString();
    }

    public void SaveTheme(string theme)
    {
        try
        {
            File.WriteAllText(SettingsPath(), Normalize(theme));
        }
        catch { }
    }

    public void ApplyTheme(string theme)
    {
        var normalized = Normalize(theme);
        if (App.MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = normalized switch
            {
                nameof(ElementTheme.Light) => ElementTheme.Light,
                nameof(ElementTheme.Dark) => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }

    private static string Normalize(string theme) => theme switch
    {
        nameof(ElementTheme.Light) => nameof(ElementTheme.Light),
        nameof(ElementTheme.Dark) => nameof(ElementTheme.Dark),
        "System" => nameof(ElementTheme.Default),
        _ => nameof(ElementTheme.Default),
    };
}
