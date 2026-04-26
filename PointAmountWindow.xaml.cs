using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Eco_Matic;

public partial class PointAmountWindow : Window
{
    private readonly int _minValue;
    private readonly int _maxValue;

    public int PointAmount { get; private set; }

    public PointAmountWindow(string title, string helpText, int initialValue, int minValue = 0, int maxValue = 999999)
    {
        InitializeComponent();
        _minValue = minValue;
        _maxValue = maxValue;
        txtTitle.Text = title;
        txtHelp.Text = helpText;
        txtPoints.Text = Math.Clamp(initialValue, minValue, maxValue).ToString(CultureInfo.InvariantCulture);
        Loaded += (_, _) =>
        {
            txtPoints.Focus();
            txtPoints.SelectAll();
        };
    }

    private void TxtPoints_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void TxtPoints_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Save();
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        Save();
    }

    private void Save()
    {
        if (!int.TryParse(txtPoints.Text.Trim(), out int amount) ||
            amount < _minValue ||
            amount > _maxValue)
        {
            MessageBox.Show(this,
                $"Enter a whole number from {_minValue} to {_maxValue}.",
                "Point Amount",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        PointAmount = amount;
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
