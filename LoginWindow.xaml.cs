using System.Windows;
using System.Windows.Input;

namespace Eco_Matic;

public partial class LoginWindow : Window
{
    public string Username { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => txtUsername.Focus();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        Username = txtUsername.Text;
        Password = txtPassword.Password;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
    {
        BtnCancel_Click(sender, e);
    }

    private void WindowFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void TxtControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnLogin_Click(sender, new RoutedEventArgs());
        }
        else if (e.Key == Key.Escape)
        {
            BtnCancel_Click(sender, new RoutedEventArgs());
        }
    }
}
