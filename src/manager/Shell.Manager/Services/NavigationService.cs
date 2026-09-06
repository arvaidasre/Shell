using Microsoft.UI.Xaml.Controls;

namespace Shell.Manager.Services;

/// <summary>
/// Thin Frame wrapper held as a singleton by <see cref="App"/>.
/// No DI container — App wires the instance to MainWindow.ContentFrame.
/// </summary>
public sealed class NavigationService
{
    private readonly Frame _frame;

    public NavigationService(Frame frame)
    {
        _frame = frame;
    }

    public bool Navigate(Type pageType, object? param = null)
    {
        if (pageType is null)
            return false;
        if (_frame.Content?.GetType() == pageType)
            return false;
        return param is null
            ? _frame.Navigate(pageType)
            : _frame.Navigate(pageType, param);
    }

    public void GoBack()
    {
        if (CanGoBack)
            _frame.GoBack();
    }

    public bool CanGoBack => _frame.CanGoBack;
}
