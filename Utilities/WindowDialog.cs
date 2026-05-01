using System.Windows;

namespace Eco_Matic.Utilities;

public static class WindowDialog
{
    public static MessageBoxResult Show(string messageBoxText)
    {
        return Show((Window?)null, messageBoxText);
    }

    public static MessageBoxResult Show(
        string messageBoxText,
        string caption)
    {
        return Show((Window?)null, messageBoxText, caption);
    }

    public static MessageBoxResult Show(
        string messageBoxText,
        string caption,
        MessageBoxButton button)
    {
        return Show((Window?)null, messageBoxText, caption, button);
    }

    public static MessageBoxResult Show(
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon)
    {
        return Show((Window?)null, messageBoxText, caption, button, icon);
    }

    public static MessageBoxResult Show(
        Window? owner,
        string messageBoxText,
        string caption = "",
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None)
    {
        Window? resolvedOwner = ResolveOwner(owner);
        if (resolvedOwner == null)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }

        if (!resolvedOwner.Dispatcher.CheckAccess())
        {
            return resolvedOwner.Dispatcher.Invoke(() =>
                Show(resolvedOwner, messageBoxText, caption, button, icon));
        }

        BringOwnerForward(resolvedOwner);
        return MessageBox.Show(resolvedOwner, messageBoxText, caption, button, icon);
    }

    private static Window? ResolveOwner(Window? owner)
    {
        if (owner is { IsLoaded: true })
        {
            return owner;
        }

        return Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive)
            ?? Application.Current?.MainWindow;
    }

    private static void BringOwnerForward(Window owner)
    {
        if (owner.WindowState == WindowState.Minimized)
        {
            owner.WindowState = WindowState.Normal;
        }

        bool originalTopmost = owner.Topmost;
        owner.Topmost = true;
        owner.Topmost = originalTopmost;
        owner.Activate();
        owner.Focus();
    }
}
