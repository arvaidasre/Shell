using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Shell.Manager.ViewModels;

namespace Shell.Manager.Views;

public sealed partial class MenusPage : Page
{
    public MenusViewModel ViewModel { get; } = new();

    public MenusPage()
    {
        InitializeComponent();
        ViewModel.Notify = (m, s) => App.MainWindow?.ShowStatus(m, (InfoBarSeverity)s);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Load();
    }

    private void IconSizeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        ViewModel.IconSize = (int)e.NewValue;
    }

    private void ShowDelaySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        ViewModel.ShowDelay = (int)e.NewValue;
    }

    private void SectionToggle_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.ToggleSwitch toggle && toggle.Tag is string id)
            ViewModel.ToggleSectionCommand.Execute(id);
    }
}
