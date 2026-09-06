using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shell.Manager.Services;
using Windows.Graphics;

namespace Shell.Manager;

public sealed partial class MainWindow : Window
{
    private bool _ready;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DragBar);

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new SizeInt32(620, 780));

        TitleText.Text = $"Shell {ShellService.ShellVersion}";
        RefreshStatus();
        SelectBoxes();
        InitMenusCard();

        _ready = true;
        _ = System.Threading.Tasks.Task.Run(ShellService.EnsureStartMenuShortcut);
    }

    private void RefreshStatus()
    {
        var registered = ShellService.IsRegistered();
        RegisteredText.Text = registered ? "● Registered" : "○ Not registered";
        TakeoverText.Text = ShellService.IsModernTakeover()
            ? "Win11 modern menu replaced (TreatAs active)"
            : "Win11 modern menu untouched";
        ConfigText.Text = "Config: " + ShellService.EffectiveConfig();
        SubtitleText.Text = registered
            ? "Right-click menus are live"
            : "Register to enable the context menu";
    }

    private void SelectBoxes()
    {
        var lang = ShellService.GetLanguage();
        foreach (ComboBoxItem item in LanguageBox.Items)
            if ((string)item.Tag == lang)
                LanguageBox.SelectedItem = item;
        ThemeBox.SelectedIndex = 0;
    }

    private void InitMenusCard()
    {
        GotoToggle.IsOn = ShellService.GetSectionEnabled("goto");
        TerminalToggle.IsOn = ShellService.GetSectionEnabled("terminal");
        DevelopToggle.IsOn = ShellService.GetSectionEnabled("develop");
        FileManageToggle.IsOn = ShellService.GetSectionEnabled("file-manage");
        IconsToggle.IsOn = ShellService.GetIconsEnabled();
        IconSizeSlider.Value = ShellService.GetIconSize();
        DelaySlider.Value = ShellService.GetShowDelay();
        TipsToggle.IsOn = ShellService.GetTipsEnabled();
    }

    private void SectionToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_ready || sender is not ToggleSwitch toggle || toggle.Tag is not string tag)
            return;
        ShellService.SetSectionEnabled(tag, toggle.IsOn);
        ShowNotice("Restart Explorer to apply.", InfoBarSeverity.Informational);
    }

    private void IconsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_ready)
            return;
        ShellService.SetIconsEnabled(IconsToggle.IsOn);
        ShowNotice("Restart Explorer to apply.", InfoBarSeverity.Informational);
    }

    private void IconSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_ready)
            return;
        ShellService.SetIconSize((int)System.Math.Round(e.NewValue));
    }

    private void DelaySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_ready)
            return;
        ShellService.SetShowDelay((int)System.Math.Round(e.NewValue));
    }

    private void TipsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_ready)
            return;
        ShellService.SetTipsEnabled(TipsToggle.IsOn);
        ShowNotice("Restart Explorer to apply.", InfoBarSeverity.Informational);
    }

    private void EditConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ShellService.EditConfig();
    }

    private void ShowNotice(string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        Notice.Message = message;
        Notice.Severity = severity;
        Notice.IsOpen = true;
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        ShowNotice("Requesting admin rights for registration…");
        var ok = ShellService.RunEngine("-register -treat -restart", elevated: true);
        RefreshStatus();
        ShowNotice(ok ? "Registered." : "Registration did not complete.",
            ok ? InfoBarSeverity.Success : InfoBarSeverity.Error);
    }

    private void UnregisterButton_Click(object sender, RoutedEventArgs e)
    {
        ShowNotice("Requesting admin rights to unregister…");
        var ok = ShellService.RunEngine("-unregister -restart", elevated: true);
        RefreshStatus();
        ShowNotice(ok ? "Unregistered." : "Unregister did not complete.",
            ok ? InfoBarSeverity.Success : InfoBarSeverity.Error);
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        ShellService.RunEngine("-restart", elevated: false);
        ShowNotice("Explorer restarted.");
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || LanguageBox.SelectedItem is not ComboBoxItem item)
            return;
        var code = (string)item.Tag;
        if (ShellService.SetLanguage(code))
            ShowNotice($"Menu language: {ShellService.LanguageName(code)}. Restart Explorer to apply.", InfoBarSeverity.Success);
        else
            ShowNotice("Could not update the language in the config file.", InfoBarSeverity.Error);
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || ThemeBox.SelectedItem is not ComboBoxItem item)
            return;
        var name = (string)item.Tag;
        if (ShellService.ApplyThemePreset(name))
            ShowNotice("Theme preset applied. Restart Explorer to apply.", InfoBarSeverity.Success);
        else
            ShowNotice("Could not apply the theme preset.", InfoBarSeverity.Error);
    }

    private void OpenConfigButton_Click(object sender, RoutedEventArgs e)
    {
        ShellService.OpenConfigFolder();
    }

    private void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var bak = ShellService.BackupConfig();
        if (bak is null)
            ShowNotice("Backup failed.", InfoBarSeverity.Error);
        else
            ShowNotice($"Config backed up to {bak}.", InfoBarSeverity.Success);
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b)
            b.IsEnabled = false;
        ShowNotice("Checking GitHub releases…");
        var (current, latest, isNew) = await ShellService.CheckForUpdatesAsync();
        if (string.IsNullOrEmpty(latest))
        {
            ShowNotice("Could not reach GitHub releases.", InfoBarSeverity.Warning);
        }
        else if (isNew)
        {
            ShowNotice($"New version available: v{latest} (installed {current}).", InfoBarSeverity.Warning);
            var dialog = new ContentDialog
            {
                Title = "Update available",
                Content = $"Shell v{latest} is available (installed {current}). Open the releases page?",
                PrimaryButtonText = "Open releases",
                CloseButtonText = "Later",
                XamlRoot = Content.XamlRoot,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                ShellService.OpenReleasesPage();
        }
        else
        {
            ShowNotice($"You have the latest version ({current}).", InfoBarSeverity.Success);
        }
        if (sender is Button b2)
            b2.IsEnabled = true;
    }

    private void ShortcutButton_Click(object sender, RoutedEventArgs e)
    {
        ShowNotice(ShellService.EnsureStartMenuShortcut()
            ? "Start Menu shortcut is in place."
            : "Could not create the Start Menu shortcut.",
            InfoBarSeverity.Informational);
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        ReportText.Text = ShellService.CheckReport();
        await ReportDialog.ShowAsync();
    }

    private void IssueButton_Click(object sender, RoutedEventArgs e)
    {
        ShellService.OpenIssuesPage();
    }

    private void ReleasesButton_Click(object sender, RoutedEventArgs e)
    {
        ShellService.OpenReleasesPage();
    }
}
