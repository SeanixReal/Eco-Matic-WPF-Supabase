using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Eco_Matic;

public partial class AdminWindow : Window
{
    private string? _selectedImagePath;

    private sealed class ProductOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public AdminWindow()
    {
        InitializeComponent();
        RefreshGrid();
        RefreshSelectors();
        LoadAddTypeSelector();
        UpdateTypeSpecificFields();
        SyncSelectorsFromGridSelection();
    }

    private void RefreshGrid()
    {
        int? selectedId = (inventoryGrid.SelectedItem as VendingItem)?.Id;
        var products = DataStore.Products.OrderBy(p => p.Id).ToList();

        inventoryGrid.ItemsSource = products;

        if (selectedId.HasValue)
        {
            var same = products.FirstOrDefault(p => p.Id == selectedId.Value);
            if (same != null)
            {
                inventoryGrid.SelectedItem = same;
            }
        }

        if (inventoryGrid.SelectedItem == null && products.Count > 0)
        {
            inventoryGrid.SelectedIndex = 0;
        }
    }

    private void LoadAddTypeSelector()
    {
        cboAddType.Items.Clear();
        cboAddType.Items.Add("Snack");
        cboAddType.Items.Add("Drink");
        cboAddType.Items.Add("Misc");

        if (cboAddType.Items.Count > 0)
        {
            cboAddType.SelectedIndex = 0;
        }

        UpdateTypeSpecificFields();
    }

    private void RefreshSelectors()
    {
        int? restockId = (cboItem.SelectedItem as ProductOption)?.Id;
        int? removeId = (cboRemoveItem.SelectedItem as ProductOption)?.Id;

        cboItem.Items.Clear();
        cboRemoveItem.Items.Clear();

        foreach (var product in DataStore.Products.OrderBy(p => p.Id))
        {
            var option = new ProductOption
            {
                Id = product.Id,
                Name = $"#{product.Id} - {product.Name}"
            };

            cboItem.Items.Add(option);
            cboRemoveItem.Items.Add(option);
        }

        if (cboItem.Items.Count == 0)
        {
            cboItem.SelectedItem = null;
            cboRemoveItem.SelectedItem = null;
            return;
        }

        SelectComboById(cboItem, restockId ?? ((ProductOption)cboItem.Items[0]).Id);
        SelectComboById(cboRemoveItem, removeId ?? ((ProductOption)cboRemoveItem.Items[0]).Id);
    }

    private static bool SelectComboById(ComboBox comboBox, int targetId)
    {
        var target = comboBox.Items.OfType<ProductOption>().FirstOrDefault(x => x.Id == targetId);
        if (target == null)
        {
            return false;
        }

        comboBox.SelectedItem = target;
        return true;
    }

    private void SelectFromGrid(int productId)
    {
        SelectComboById(cboItem, productId);
        SelectComboById(cboRemoveItem, productId);
    }

    private void SyncSelectorsFromGridSelection()
    {
        if (inventoryGrid.SelectedItem is VendingItem product)
        {
            SelectFromGrid(product.Id);
        }
    }

    private void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (cboItem.SelectedItem is not ProductOption selected)
        {
            MessageBox.Show(this, "Please select an item.", "Restock", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var product = DataStore.Products.FirstOrDefault(p => p.Id == selected.Id);
        if (product == null)
        {
            return;
        }

        int oldStock = product.Stock;
        product.Stock = DataStore.MaxStockPerItem;

        DataStore.SaveInventory();
        DataStore.LogEvent("ADMIN_RESTOCK", $"{product.Name}: {oldStock} -> {product.Stock}");

        MessageBox.Show(this,
            $"{product.Name} restocked to {DataStore.MaxStockPerItem}.",
            "Restock",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        RefreshGrid();
        RefreshSelectors();
        SelectFromGrid(product.Id);
    }

    private void BtnRestockAdd_Click(object sender, RoutedEventArgs e)
    {
        if (cboItem.SelectedItem is not ProductOption selected)
        {
            return;
        }

        var product = DataStore.Products.FirstOrDefault(p => p.Id == selected.Id);
        if (product == null)
        {
            return;
        }

        if (!TryParseInt(txtRestockQty.Text, out int qty) || qty <= 0)
        {
            MessageBox.Show(this,
                "Quantity must be a positive whole number.",
                "Restock",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        int oldStock = product.Stock;
        product.Stock = Math.Clamp(product.Stock + qty, 0, DataStore.MaxStockPerItem);

        DataStore.SaveInventory();
        DataStore.LogEvent("ADMIN_RESTOCK_ADD", $"{product.Name}: {oldStock} +{qty} -> {product.Stock}");

        MessageBox.Show(this,
            $"{product.Name}: {oldStock} -> {product.Stock}.",
            "Restock",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        RefreshGrid();
        RefreshSelectors();
        SelectFromGrid(product.Id);
    }

    private void BtnAddItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataStore.Products.Count >= DataStore.MaxItemSlots)
        {
            MessageBox.Show(this,
                $"Max slots ({DataStore.MaxItemSlots}) reached.",
                "Add Item",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string name = txtAddName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "Name is required.", "Add Item", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DataStore.Products.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "Name must be unique.", "Add Item", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboAddType.SelectedItem is not string type)
        {
            MessageBox.Show(this, "Select a product type.", "Add Item", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseDecimal(txtAddPrice.Text, out decimal price) || price <= 0)
        {
            MessageBox.Show(this, "Price must be greater than zero.", "Add Item", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseInt(txtAddStock.Text, out int stock) || stock <= 0)
        {
            MessageBox.Show(this, "Stock must be a positive whole number.", "Add Item", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        stock = Math.Clamp(stock, 1, DataStore.MaxStockPerItem);

        string flavor = string.IsNullOrWhiteSpace(txtAddFlavor.Text)
            ? "No description available."
            : txtAddFlavor.Text.Trim();

        int calories = 0;
        int volumeMl = 0;

        if (type == "Snack" && !TryParseOptionalNonNegativeInt(txtAddCalories.Text, out calories))
        {
            MessageBox.Show(this, "Calories must be 0 or a positive whole number.", "Add Item", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (type == "Drink" && !TryParseOptionalNonNegativeInt(txtAddVolume.Text, out volumeMl))
        {
            MessageBox.Show(this, "Volume must be 0 or a positive whole number.", "Add Item", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int newId = DataStore.Products.Count == 0 ? 1 : DataStore.Products.Max(p => p.Id) + 1;

        VendingItem newProduct = type switch
        {
            "Snack" => new SnackItem { Id = newId, Name = name, Price = price, Stock = stock, FlavorText = flavor, Calories = calories },
            "Drink" => new DrinkItem { Id = newId, Name = name, Price = price, Stock = stock, FlavorText = flavor, VolumeMl = volumeMl },
            _ => new MiscItem { Id = newId, Name = name, Price = price, Stock = stock, FlavorText = flavor }
        };

        if (!string.IsNullOrWhiteSpace(_selectedImagePath) && File.Exists(_selectedImagePath))
        {
            try
            {
                string relativeName = CsvStorage.CopyProductImage(_selectedImagePath!, newId);
                newProduct.ImagePath = relativeName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Image copy warning: {ex.Message}",
                    "Add Item",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        DataStore.Products.Add(newProduct);
        DataStore.SaveInventory();
        DataStore.LogEvent("ADMIN_ADD_ITEM", $"{newProduct.Name} ({newProduct.Type})", newProduct.Price);

        MessageBox.Show(this, "Item added.", "Add Item", MessageBoxButton.OK, MessageBoxImage.Information);

        ResetAddFields();
        RefreshGrid();
        RefreshSelectors();
        SelectFromGrid(newProduct.Id);
    }

    private void ResetAddFields()
    {
        txtAddName.Clear();
        txtAddFlavor.Clear();
        txtAddPrice.Text = "1";
        txtAddStock.Text = DataStore.MaxStockPerItem.ToString(CultureInfo.InvariantCulture);
        txtAddCalories.Text = "0";
        txtAddVolume.Text = "0";

        _selectedImagePath = null;
        lblImagePath.Text = "No image selected";
        picImagePreview.Source = null;

        UpdateTypeSpecificFields();
    }

    private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (cboRemoveItem.SelectedItem is not ProductOption selected)
        {
            return;
        }

        var product = DataStore.Products.FirstOrDefault(p => p.Id == selected.Id);
        if (product == null)
        {
            return;
        }

        var confirm = MessageBox.Show(this,
            $"Remove {product.Name}?",
            "Confirm",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        DataStore.Products.Remove(product);
        DataStore.SaveInventory();
        DataStore.LogEvent("ADMIN_REMOVE_ITEM", product.Name);

        RefreshGrid();
        RefreshSelectors();
        SyncSelectorsFromGridSelection();
    }

    private void BtnViewLog_Click(object sender, RoutedEventArgs e)
    {
        var logWindow = new EventLogWindow
        {
            Owner = this
        };
        logWindow.ShowDialog();
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(this,
            "Clear all logs?",
            "Confirm",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        DataStore.ClearLogs();
        MessageBox.Show(this, "Log cleared.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnOpenReadme_Click(object sender, RoutedEventArgs e)
    {
        var readme = new ReadmeWindow
        {
            Owner = this
        };
        readme.ShowDialog();
    }

    private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Product Image",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All Files|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _selectedImagePath = dialog.FileName;
        lblImagePath.Text = Path.GetFileName(_selectedImagePath);

        var image = ImageLoader.LoadFromPath(_selectedImagePath);
        if (image == null)
        {
            _selectedImagePath = null;
            picImagePreview.Source = null;
            lblImagePath.Text = "Invalid image";
            return;
        }

        picImagePreview.Source = image;
    }

    private void CboAddType_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTypeSpecificFields();
    }

    private void UpdateTypeSpecificFields()
    {
        if (cboAddType.SelectedItem is not string type)
        {
            return;
        }

        bool showCalories = type == "Snack";
        bool showVolume = type == "Drink";

        lblAddCalories.Visibility = showCalories ? Visibility.Visible : Visibility.Collapsed;
        txtAddCalories.Visibility = showCalories ? Visibility.Visible : Visibility.Collapsed;

        lblAddVolume.Visibility = showVolume ? Visibility.Visible : Visibility.Collapsed;
        txtAddVolume.Visibility = showVolume ? Visibility.Visible : Visibility.Collapsed;

        if (!showCalories)
        {
            txtAddCalories.Text = "0";
        }

        if (!showVolume)
        {
            txtAddVolume.Text = "0";
        }
    }

    private void BtnAdminHelp_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "Admin Tools:\n\n" +
            "- Restock items to max or add custom quantities.\n" +
            "- Add and remove products with optional images.\n" +
            "- View and clear event logs.\n\n" +
            "Low stock items (2 or less) are highlighted in the grid.",
            "Admin Help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void InventoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SyncSelectorsFromGridSelection();
    }

    private void InventoryGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is VendingItem product && product.Stock <= 2)
        {
            e.Row.Foreground = Brushes.OrangeRed;
            e.Row.FontWeight = FontWeights.SemiBold;
        }
        else
        {
            e.Row.Foreground = new SolidColorBrush(Color.FromRgb(38, 52, 77));
            e.Row.FontWeight = FontWeights.Normal;
        }
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
        {
            return true;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
               || int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result);
    }

    private static bool TryParseOptionalNonNegativeInt(string value, out int result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            return true;
        }

        if (!TryParseInt(value, out result))
        {
            return false;
        }

        return result >= 0;
    }
}
