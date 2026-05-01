using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Eco_Matic.Utilities;

namespace Eco_Matic
{
    public partial class InventoryItemWindow : Window
    {
        public string SlotId { get; private set; } = string.Empty;
        public int InitialStock { get; private set; }
        public int MaxCapacity { get; private set; } = DataStore.MaxStockPerItem;
        public decimal? SlotPriceOverride { get; private set; }
        public int? SelectedItemId { get; private set; }
        private readonly int? _initialItemId;
        private readonly bool _isRequiredSetupMode;
        private readonly int _minimumRequiredSlots;
        private readonly int _completedSlots;

        public InventoryItemWindow()
        {
            InitializeComponent();
            btnSave.IsEnabled = false;
            cboExistingItem.IsEnabled = false;
        }

        public InventoryItemWindow(string suggestedSlotId, int completedSlots, int minimumRequiredSlots) : this()
        {
            _isRequiredSetupMode = true;
            _minimumRequiredSlots = minimumRequiredSlots;
            _completedSlots = completedSlots;
            TitleContent.Text = "Required Machine Setup";
            txtSlotId.Text = SlotIdHelper.Normalize(suggestedSlotId) ?? suggestedSlotId;
            btnSave.Content = "Save Slot";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCatalogAsync();
        }

        private async Task LoadCatalogAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataView view = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.GetAllItems().DefaultView;
                });

                cboExistingItem.ItemsSource = view;

                if (_initialItemId.HasValue)
                {
                    foreach (DataRowView rowView in cboExistingItem.Items)
                    {
                        if (Convert.ToInt32(rowView.Row["item_id"]) == _initialItemId.Value)
                        {
                            cboExistingItem.SelectedItem = rowView;
                            break;
                        }
                    }
                }

                txtCatalogStatus.Text = view.Count == 0
                    ? "No global items found yet. Add items in the Global Items view first."
                    : "Choose a global item to preview and assign to this slot.";

                if (_isRequiredSetupMode && view.Count > 0)
                {
                    txtCatalogStatus.Text = $"Required setup in progress: {_completedSlots}/{_minimumRequiredSlots} slots completed.";
                }
            }
            catch (Exception ex)
            {
                txtCatalogStatus.Text = "Failed to load global items.";
                MessageBox.Show(this,
                    $"Could not load the global item catalog.\n\n{ex.Message}",
                    "Catalog Load Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                cboExistingItem.IsEnabled = true;
                btnSave.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CboExistingItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboExistingItem.SelectedItem is not DataRowView rowView)
            {
                SelectedItemId = null;
                txtName.Text = "";
                txtType.Text = "";
                txtDefaultPrice.Text = "";
                txtImagePath.Text = "";
                txtDispenseMessage.Text = "";
                txtExamineMessage.Text = "";
                return;
            }

            var row = rowView.Row;
            SelectedItemId = Convert.ToInt32(row["item_id"]);
            txtName.Text = row["name"].ToString() ?? "";
            txtType.Text = row["type"].ToString() ?? "";
            txtDefaultPrice.Text = Convert.ToDecimal(row["price"]).ToString("0.00", CultureInfo.InvariantCulture);
            txtImagePath.Text = row["image_path"].ToString() ?? "Assets/Images/placeholder.png";
            txtDispenseMessage.Text = row["dispense_message"].ToString() ?? "Enjoy your item!";
            txtExamineMessage.Text = row["examine_message"].ToString() ?? "A standard vending item.";
        }

        public InventoryItemWindow(string slotId, int itemId, int stock, decimal? slotPrice) : this()
        {
            _initialItemId = itemId;
            TitleContent.Text = "Edit Machine Slot";
            txtSlotId.Text = SlotIdHelper.Normalize(slotId) ?? slotId;
            txtStock.Text = stock.ToString(CultureInfo.InvariantCulture);
            txtSlotPrice.Text = slotPrice.HasValue ? slotPrice.Value.ToString("0.00", CultureInfo.InvariantCulture) : "";
        }

        private void WindowFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!SlotIdHelper.TryGetSlotNumber(txtSlotId.Text, out _))
            {
                MessageBox.Show("Slot ID must be a number from 1 to 12.", "Invalid Slot", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cboExistingItem.SelectedItem is not DataRowView selectedRowView)
            {
                MessageBox.Show("Please select a global item for this slot.", "Missing Item", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedItemId = Convert.ToInt32(selectedRowView.Row["item_id"]);

            if (!int.TryParse(txtStock.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stock) || stock < 0)
            {
                MessageBox.Show("Stock must be a valid non-negative integer.", "Invalid Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (stock > DataStore.MaxStockPerItem)
            {
                MessageBox.Show($"Stock cannot exceed {DataStore.MaxStockPerItem}.", "Invalid Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtSlotPrice.Text) &&
                !decimal.TryParse(txtSlotPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Machine item price must be a valid number or left blank.", "Invalid Price", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SlotId = SlotIdHelper.Normalize(txtSlotId.Text) ?? txtSlotId.Text.Trim();
            InitialStock = int.TryParse(txtStock.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedStock) ? parsedStock : 0;
            MaxCapacity = DataStore.MaxStockPerItem;
            SlotPriceOverride = string.IsNullOrWhiteSpace(txtSlotPrice.Text)
                ? null
                : decimal.TryParse(txtSlotPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price)
                    ? price
                    : null;

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
