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
            var parentDir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
            if (parentDir is not null)
            {
                var upOne = Path.Combine(parentDir, "shell.exe");
                if (File.Exists(upOne))
                    return upOne;
            }
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

    public static readonly (string Id, string Name)[] MenuSections = new (string Id, string Name)[]
    {
        ("goto", "Go to"),
        ("terminal", "Terminal"),
        ("develop", "Development"),
        ("file-manage", "File management"),
    };

    public static bool GetSectionEnabled(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
                return false;
            var cfg = EffectiveConfig();
            if (!File.Exists(cfg))
                return false;
            var needle = "imports/" + id + ".nss";
            foreach (var line in File.ReadAllLines(cfg, new UTF8Encoding(false)))
            {
                var t = line.TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal))
                    continue;
                if (!t.StartsWith("import", StringComparison.Ordinal))
                    continue;
                if (t.Contains(needle, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public static void SetSectionEnabled(string id, bool on)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
                return;
            var cfg = EffectiveConfig();
            if (!File.Exists(cfg))
                return;
            var enc = new UTF8Encoding(false);
            var lines = File.ReadAllLines(cfg, enc).ToList();
            var needle = "imports/" + id + ".nss";
            var found = false;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();
                var code = trimmed;
                if (code.StartsWith("//", StringComparison.Ordinal))
                    code = code.Substring(2).TrimStart();
                if (!code.StartsWith("import", StringComparison.Ordinal))
                    continue;
                if (!code.Contains(needle, StringComparison.Ordinal))
                    continue;
                found = true;
                var indent = line.Substring(0, line.Length - trimmed.Length);
                lines[i] = on ? indent + "import 'imports/" + id + ".nss'"
                              : indent + "// import 'imports/" + id + ".nss'";
            }
            if (!found && on)
            {
                File.AppendAllText(cfg, "\r\nimport 'imports/" + id + ".nss'\r\n", enc);
                return;
            }
            if (!found)
                return;
            File.WriteAllLines(cfg, lines, enc);
        }
        catch { }
    }

    private static string? ThemePath()
    {
        try
        {
            var dir = Path.GetDirectoryName(EffectiveConfig());
            if (dir is null)
                return null;
            return Path.Combine(dir, "imports", "theme.nss");
        }
        catch
        {
            return null;
        }
    }

    private static string? GetThemeDotted(string key)
    {
        try
        {
            var path = ThemePath();
            if (path is null || !File.Exists(path))
                return null;
            foreach (var line in File.ReadAllLines(path, new UTF8Encoding(false)))
            {
                var t = line.TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal))
                    continue;
                if (!t.StartsWith(key, StringComparison.Ordinal))
                    continue;
                var rest = t.Substring(key.Length).TrimStart();
                if (!rest.StartsWith("=", StringComparison.Ordinal))
                    continue;
                var value = rest.Substring(1).Trim();
                value = value.TrimEnd(';').Trim();
                var end = value.IndexOfAny(new[] { ' ', '\t', '/' });
                if (end >= 0)
                    value = value.Substring(0, end).Trim();
                return value;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static void SetThemeDotted(string key, string value)
    {
        try
        {
            var path = ThemePath();
            if (path is null || !File.Exists(path))
                return;
            var enc = new UTF8Encoding(false);
            var lines = File.ReadAllLines(path, enc).ToList();
            for (var i = 0; i < lines.Count; i++)
            {
                var t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal))
                    continue;
                if (!t.StartsWith(key, StringComparison.Ordinal))
                    continue;
                var rest = t.Substring(key.Length).TrimStart();
                if (!rest.StartsWith("=", StringComparison.Ordinal))
                    continue;
                var indent = lines[i].Substring(0, lines[i].Length - t.Length);
                lines[i] = indent + key + " = " + value;
                File.WriteAllLines(path, lines, enc);
                return;
            }
            var alignIdx = -1;
            for (var i = 0; i < lines.Count; i++)
            {
                var t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal))
                    continue;
                if (!t.StartsWith("image.align", StringComparison.Ordinal))
                    continue;
                var rest = t.Substring("image.align".Length).TrimStart();
                if (!rest.StartsWith("=", StringComparison.Ordinal))
                    continue;
                alignIdx = i;
                break;
            }
            var newIndent = "\t";
            if (alignIdx >= 0)
            {
                var a = lines[alignIdx];
                var at = a.TrimStart();
                newIndent = a.Substring(0, a.Length - at.Length);
                lines.Insert(alignIdx + 1, newIndent + key + " = " + value);
            }
            else
            {
                var closeIdx = -1;
                for (var i = lines.Count - 1; i >= 0; i--)
                {
                    if (lines[i].Trim() == "}")
                    {
                        closeIdx = i;
                        break;
                    }
                }
                if (closeIdx < 0)
                {
                    for (var i = lines.Count - 1; i >= 0; i--)
                    {
                        if (lines[i].Contains("}", StringComparison.Ordinal))
                        {
                            closeIdx = i;
                            break;
                        }
                    }
                }
                if (closeIdx >= 0)
                    lines.Insert(closeIdx, newIndent + key + " = " + value);
                else
                    lines.Add(newIndent + key + " = " + value);
            }
            File.WriteAllLines(path, lines, enc);
        }
        catch { }
    }

    public static bool GetIconsEnabled()
    {
        try
        {
            var v = GetThemeDotted("image.enabled");
            if (v is null)
                return true;
            if (v.Equals("1", StringComparison.OrdinalIgnoreCase)
                || v.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (v.Equals("0", StringComparison.OrdinalIgnoreCase)
                || v.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }
        catch
        {
            return true;
        }
    }

    public static void SetIconsEnabled(bool on)
    {
        try
        {
            SetThemeDotted("image.enabled", on ? "1" : "0");
        }
        catch { }
    }

    public static int GetIconSize()
    {
        try
        {
            var v = GetThemeDotted("image.size");
            if (v is null)
                return 16;
            if (int.TryParse(v, out var n))
                return n;
            return 16;
        }
        catch
        {
            return 16;
        }
    }

    public static void SetIconSize(int px)
    {
        try
        {
            if (px < 12)
                px = 12;
            if (px > 32)
                px = 32;
            SetThemeDotted("image.size", px.ToString());
        }
        catch { }
    }

    public static int GetShowDelay()
    {
        try
        {
            var cfg = EffectiveConfig();
            if (!File.Exists(cfg))
                return 150;
            foreach (var line in File.ReadAllLines(cfg, new UTF8Encoding(false)))
            {
                var t = line.TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal))
                    continue;
                var idx = t.IndexOf("showdelay", StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                var eq = t.IndexOf('=', idx);
                if (eq < 0)
                    continue;
                var after = t.Substring(eq + 1).Trim();
                var j = 0;
                if (j < after.Length && (after[j] == '-' || after[j] == '+'))
                    j++;
                var k = j;
                while (k < after.Length && char.IsDigit(after[k]))
                    k++;
                if (k == j)
                    continue;
                if (int.TryParse(after.Substring(0, k), out var n))
                    return n;
            }
            return 150;
        }
        catch
        {
            return 150;
        }
    }

    public static void SetShowDelay(int ms)
    {
        try
        {
            if (ms < 0)
                ms = 0;
            if (ms > 1000)
                ms = 1000;
            var cfg = EffectiveConfig();
            if (!File.Exists(cfg))
                return;
            var enc = new UTF8Encoding(false);
            var lines = File.ReadAllLines(cfg, enc).ToList();
            var changed = false;
            for (var i = 0; i < lines.Count; i++)
            {
                var t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal))
                    continue;
                var idx = t.IndexOf("showdelay", StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                var eq = lines[i].IndexOf('=', idx);
                if (eq < 0)
                    continue;
                var prefix = lines[i].Substring(0, eq + 1);
                var suffix = lines[i].Substring(eq + 1);
                var s = 0;
                while (s < suffix.Length && !char.IsDigit(suffix[s]) && suffix[s] != '-' && suffix[s] != '+')
                    s++;
                if (s >= suffix.Length)
                    continue;
                var e = s;
                if (suffix[e] == '-' || suffix[e] == '+')
                    e++;
                while (e < suffix.Length && char.IsDigit(suffix[e]))
                    e++;
                if (e == s || (e == s + 1 && !char.IsDigit(suffix[s])))
                    continue;
                lines[i] = prefix + suffix.Substring(0, s) + ms.ToString() + suffix.Substring(e);
                changed = true;
                break;
            }
            if (changed)
                File.WriteAllLines(cfg, lines, enc);
        }
        catch { }
    }

    public static bool GetTipsEnabled()
    {
        try
        {
            var cfg = EffectiveConfig();
            if (!File.Exists(cfg))
                return true;
            foreach (var line in File.ReadAllLines(cfg, new UTF8Encoding(false)))
            {
                var t = line.TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal))
                    continue;
                var idx = t.IndexOf("tip.enabled", StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                var eq = t.IndexOf('=', idx);
                if (eq < 0)
                    continue;
                var value = t.Substring(eq + 1).Trim().TrimEnd(';').Trim();
                var end = value.IndexOfAny(new[] { ' ', '\t', '/' });
                if (end >= 0)
                    value = value.Substring(0, end).Trim();
                if (value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("1", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (value.Equals("false", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("0", StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    public static void SetTipsEnabled(bool on)
    {
        try
        {
            var cfg = EffectiveConfig();
            if (!File.Exists(cfg))
                return;
            var enc = new UTF8Encoding(false);
            var lines = File.ReadAllLines(cfg, enc).ToList();
            var changed = false;
            for (var i = 0; i < lines.Count; i++)
            {
                var t = lines[i].TrimStart();
                if (t.StartsWith("//", StringComparison.Ordinal))
                    continue;
                var idx = t.IndexOf("tip.enabled", StringComparison.Ordinal);
                if (idx < 0)
                    continue;
                var eq = lines[i].IndexOf('=', idx);
                if (eq < 0)
                    continue;
                var prefix = lines[i].Substring(0, eq + 1);
                var suffix = lines[i].Substring(eq + 1);
                var s = 0;
                while (s < suffix.Length && (suffix[s] == ' ' || suffix[s] == '\t'))
                    s++;
                var e = s;
                while (e < suffix.Length && char.IsLetterOrDigit(suffix[e]))
                    e++;
                var replacement = on ? "true" : "false";
                lines[i] = e > s ? prefix + suffix.Substring(0, s) + replacement + suffix.Substring(e)
                                 : prefix + suffix.Substring(0, s) + replacement;
                changed = true;
                break;
            }
            if (changed)
                File.WriteAllLines(cfg, lines, enc);
        }
        catch { }
    }

    public static void EditConfig()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = "\"" + EffectiveConfig() + "\"",
                UseShellExecute = true,
            });
        }
        catch { }
    }
}
