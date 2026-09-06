using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace Shell.Manager.Services;

/// <summary>
/// Talks to the native engine (shell.exe CLI + shell.nss config + registry).
/// The WinUI app is a GUI frontend; registration work stays in the engine
/// so there is exactly one implementation of it.
/// </summary>
internal static class ShellService
{
    private const string HandlerSubkey = @"*\shellex\ContextMenuHandlers\ @nilesoft.shell";
    private const string ShellClsid = "{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}";
    private const string ExplorerClsid = "{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";
    private const string ReleasesUrl = "https://github.com/arvaidasre/Shell/releases";
    private const string LatestApi = "https://api.github.com/repos/arvaidasre/Shell/releases/latest";

    public static string InstallDir
    {
        get
        {
            var exe = ShellExe;
            if (exe is not null)
                return Path.GetDirectoryName(exe)!;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Shell");
        }
    }

    public static string? ShellExe
    {
        get
        {
            var nextToMe = Path.Combine(AppContext.BaseDirectory, "shell.exe");
            if (File.Exists(nextToMe))
                return nextToMe;
            var local = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Shell", "shell.exe");
            return File.Exists(local) ? local : null;
        }
    }

    public static string ShellVersion
    {
        get
        {
            var exe = ShellExe;
            if (exe is null)
                return "unknown";
            try
            {
                var v = FileVersionInfo.GetVersionInfo(exe);
                return $"{v.FileMajorPart}.{v.FileMinorPart}.{v.FileBuildPart}";
            }
            catch
            {
                return "unknown";
            }
        }
    }

    /// <summary>Mirrors the dll resolution order: registry override, %AppData%, install dir.</summary>
    public static string EffectiveConfig()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Nilesoft\Shell");
            var cfg = key?.GetValue("config") as string;
            if (!string.IsNullOrEmpty(cfg) && File.Exists(cfg))
                return cfg;
        }
        catch { }

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Nilesoft", "Shell", "shell.nss");
        if (File.Exists(appData))
            return appData;

        return Path.Combine(InstallDir, "shell.nss");
    }

    public static string GetLanguage()
    {
        var cfg = EffectiveConfig();
        try
        {
            var bytes = File.ReadAllBytes(cfg);
            var text = Encoding.Latin1.GetString(bytes);
            foreach (var token in new[] { "$ui_lang", "$lang" })
            {
                var i = text.IndexOf(token, StringComparison.Ordinal);
                if (i < 0)
                    continue;
                var q = text.IndexOf('"', i);
                if (q > 0 && q + 3 < text.Length && text[q + 3] == '"'
                    && char.IsAsciiLetter(text[q + 1]) && char.IsAsciiLetter(text[q + 2]))
                    return text.Substring(q + 1, 2);
            }
        }
        catch { }
        return "en";
    }

    /// <summary>Byte-level 2-letter swap (encoding-safe); inserts the line if missing.</summary>
    public static bool SetLanguage(string code)
    {
        if (code.Length != 2)
            return false;
        var cfg = EffectiveConfig();
        try
        {
            var bytes = File.ReadAllBytes(cfg);
            var text = Encoding.Latin1.GetString(bytes);
            var i = text.IndexOf("$ui_lang", StringComparison.Ordinal);
            if (i < 0)
                i = text.IndexOf("$lang", StringComparison.Ordinal);
            if (i >= 0)
            {
                var q = text.IndexOf('"', i);
                if (q > 0 && q + 3 < text.Length && text[q + 3] == '"'
                    && char.IsAsciiLetter(text[q + 1]) && char.IsAsciiLetter(text[q + 2]))
                {
                    var chars = text.ToCharArray();
                    chars[q + 1] = code[0];
                    chars[q + 2] = code[1];
                    File.WriteAllBytes(cfg, Encoding.Latin1.GetBytes(chars));
                    return true;
                }
                return false;
            }
            if (bytes.Contains((byte)0))
                return false; // non-single-byte file, don't touch
            var line = $"$ui_lang = \"{code}\";\r\n";
            var outBytes = Encoding.Latin1.GetBytes(line).Concat(bytes).ToArray();
            File.WriteAllBytes(cfg, outBytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsRegistered()
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(HandlerSubkey);
            if (key is null)
                return false;
            return string.Equals(key.GetValue("") as string, ShellClsid,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsModernTakeover()
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey($@"CLSID\{ExplorerClsid}\TreatAs");
            return string.Equals(key?.GetValue("") as string, ShellClsid,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Runs the engine CLI. Registration needs admin → UAC prompt.</summary>
    public static bool RunEngine(string arguments, bool elevated)
    {
        var exe = ShellExe;
        if (exe is null)
            return false;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                UseShellExecute = true,
            };
            if (elevated)
                psi.Verb = "runas";
            using var p = Process.Start(psi);
            p?.WaitForExit(120_000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void OpenConfigFolder()
    {
        var dir = Path.GetDirectoryName(EffectiveConfig()) ?? InstallDir;
        Process.Start(new ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true,
        });
    }

    public static string? BackupConfig()
    {
        try
        {
            var cfg = EffectiveConfig();
            var dir = Path.GetDirectoryName(cfg);
            if (dir is null || !Directory.Exists(dir))
                return null;
            var parent = Path.GetDirectoryName(dir) ?? dir;
            var bak = Path.Combine(parent, $"config-backup-{DateTime.Now:yyyyMMdd-HHmm}");
            CopyTree(dir, bak);
            return bak;
        }
        catch
        {
            return null;
        }
    }

    private static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from))
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in Directory.GetDirectories(from))
            CopyTree(dir, Path.Combine(to, Path.GetFileName(dir)));
    }

    public static bool ApplyThemePreset(string name)
    {
        try
        {
            var dir = Path.GetDirectoryName(EffectiveConfig());
            if (dir is null)
                return false;
            string? src = null;
            if (name == "default")
            {
                src = Path.Combine(InstallDir, "imports", "theme.nss");
            }
            else
            {
                var local = Path.Combine(dir, "imports", "themes", name + ".nss");
                src = File.Exists(local)
                    ? local
                    : Path.Combine(InstallDir, "imports", "themes", name + ".nss");
            }
            if (!File.Exists(src))
                return false;
            var destDir = Path.Combine(dir, "imports");
            Directory.CreateDirectory(destDir);
            File.Copy(src, Path.Combine(destDir, "theme.nss"), overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<(string Current, string Latest, bool IsNew)> CheckForUpdatesAsync()
    {
        var current = ShellVersion;
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Shell-Manager/1.0");
            http.Timeout = TimeSpan.FromSeconds(15);
            var json = await http.GetStringAsync(LatestApi);
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var latest = tag.StartsWith("v") ? tag[1..] : tag;
            return (current, latest, !string.IsNullOrEmpty(latest) && latest != current);
        }
        catch
        {
            return (current, "", false);
        }
    }

    public static void OpenReleasesPage()
    {
        Process.Start(new ProcessStartInfo { FileName = ReleasesUrl, UseShellExecute = true });
    }

    public static void OpenIssuesPage()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/arvaidasre/Shell/issues",
            UseShellExecute = true
        });
    }

    public static bool EnsureStartMenuShortcut()
    {
        try
        {
            var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var lnk = Path.Combine(programs, "Shell Manager.lnk");
            if (File.Exists(lnk))
                return true;
            var target = Environment.ProcessPath;
            if (target is null)
                return false;
            dynamic shell = Activator.CreateInstance(
                Type.GetTypeFromProgID("WScript.Shell")!)!;
            dynamic shortcut = shell.CreateShortcut(lnk);
            shortcut.TargetPath = target;
            shortcut.WorkingDirectory = Path.GetDirectoryName(target);
            shortcut.Description = "Shell - context menu manager";
            shortcut.Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string CheckReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Shell {ShellVersion}");
        var exe = ShellExe;
        sb.AppendLine($"Engine: {(exe ?? "(not found)")}");
        sb.AppendLine($"Registered: {(IsRegistered() ? "yes" : "no")}");
        sb.AppendLine($"Win11 modern takeover (TreatAs): {(IsModernTakeover() ? "yes" : "no")}");
        var cfg = EffectiveConfig();
        sb.AppendLine($"Config: {cfg} ({(File.Exists(cfg) ? "found" : "MISSING")})");
        sb.AppendLine($"Menu language: {LanguageName(GetLanguage())}");
        return sb.ToString();
    }

    public static string LanguageName(string code) => code switch
    {
        "lt" => "Lietuvių",
        "ru" => "Русский",
        _ => "English",
    };

    public static string NextLanguage(string code) => code switch
    {
        "en" => "lt",
        "lt" => "ru",
        _ => "en",
    };
}
