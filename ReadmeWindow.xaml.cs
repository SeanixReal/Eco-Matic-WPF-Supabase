using System.IO;
using System.Windows;

namespace Eco_Matic;

public partial class ReadmeWindow : Window
{
    private const string DefaultReadme = @"ECO-MATIC VENDING MACHINE

About
Eco-Matic is a WPF vending machine simulator built with C# and .NET.
It features a customer purchase interface, admin inventory management,
receipt generation, and CSV persistence.

How to Use

Customer Mode
1. Click Customer on the home page.
2. Insert money using denomination buttons.
3. Select an available product.
4. Optionally add balance using Recycle for Credit.
5. Use Examine to view product details.
6. Use Coin Return to refund inserted money.

Admin Mode
1. Click Admin on the home page.
2. Enter password: admin123.
3. Restock items to max or add custom quantity.
4. Add or remove inventory items.
5. View or clear event logs.

Technical Details
- Framework: .NET WPF
- Language: C#
- Data Storage: CSV files under data folder
- Architecture: Multi-window with shared DataStore

Author
Copyright 2026 Seanix";

    public ReadmeWindow()
    {
        InitializeComponent();
        txtContent.Text = LoadReadmeText();
        txtContent.SelectionStart = 0;
        txtContent.SelectionLength = 0;
    }

    private static string LoadReadmeText()
    {
        string baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "README.md"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "README.md")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "README.md"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch
                {
                    return DefaultReadme;
                }
            }
        }

        return DefaultReadme;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnOpenAbout_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow
        {
            Owner = Owner ?? this
        };
        about.ShowDialog();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void WindowFrame_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
