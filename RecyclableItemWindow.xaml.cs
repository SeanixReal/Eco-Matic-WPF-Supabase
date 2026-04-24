using System.Globalization;
using System.Windows;

namespace Eco_Matic;

public partial class RecyclableItemWindow : Window
{
    public string DisplayNameValue { get; private set; } = string.Empty;
    public string MaterialType { get; private set; } = string.Empty;
    public string UnitLabel { get; private set; } = string.Empty;
    public int PointsPerUnit { get; private set; }
    public int SortOrder { get; private set; }
    public string DescriptionValue { get; private set; } = string.Empty;
    public bool IsActiveValue { get; private set; } = true;

    public RecyclableItemWindow()
    {
        InitializeComponent();
        txtPointsPerUnit.Text = "1";
        txtSortOrder.Text = "1";
        txtUnitLabel.Text = "piece";
    }

    public RecyclableItemWindow(RecyclableItemDefinition item) : this()
    {
        txtDisplayName.Text = item.DisplayName;
        txtMaterialType.Text = item.MaterialType;
        txtUnitLabel.Text = item.UnitLabel;
        txtPointsPerUnit.Text = item.PointsPerUnit.ToString(CultureInfo.InvariantCulture);
        txtSortOrder.Text = item.SortOrder.ToString(CultureInfo.InvariantCulture);
        txtDescription.Text = item.Description;
        chkIsActive.IsChecked = item.IsActive;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtDisplayName.Text))
        {
            MessageBox.Show(this, "Display name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtMaterialType.Text))
        {
            MessageBox.Show(this, "Material type is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtUnitLabel.Text))
        {
            MessageBox.Show(this, "Unit label is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(txtPointsPerUnit.Text, out int pointsPerUnit) || pointsPerUnit <= 0)
        {
            MessageBox.Show(this, "Points per unit must be a whole number greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(txtSortOrder.Text, out int sortOrder) || sortOrder < 0)
        {
            MessageBox.Show(this, "Sort order must be zero or greater.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DisplayNameValue = txtDisplayName.Text.Trim();
        MaterialType = txtMaterialType.Text.Trim();
        UnitLabel = txtUnitLabel.Text.Trim();
        PointsPerUnit = pointsPerUnit;
        SortOrder = sortOrder;
        DescriptionValue = txtDescription.Text.Trim();
        IsActiveValue = chkIsActive.IsChecked == true;

        DialogResult = true;
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void WindowFrame_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow
        {
            Owner = this
        };
        about.ShowDialog();
    }

    private void OpenReadmeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var readme = new ReadmeWindow
        {
            Owner = this
        };
        readme.ShowDialog();
    }
}
