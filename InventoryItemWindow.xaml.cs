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
        public InventoryItemWindow()
        {
            InitializeComponent();
            LoadCatalog();
        }

        private void LoadCatalog()
        {
            var store = new Data.SupabaseStore();
            cboExistingItem.ItemsSource = store.GetAllItems().DefaultView;
        }

        private void CboExistingItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboExistingItem.SelectedItem is not DataRowView rowView)
            {
                txtName.Text = "";
                txtType.Text = "";
                txtDefaultPrice.Text = "";
                txtImagePath.Text = "";
                txtDispenseMessage.Text = "";
                txtExamineMessage.Text = "";
                return;
            }

            var row = rowView.Row;
            txtName.Text = row["name"].ToString() ?? "";
            txtType.Text = row["type"].ToString() ?? "";
            txtDefaultPrice.Text = Convert.ToDecimal(row["price"]).ToString("0.00", CultureInfo.InvariantCulture);
            txtImagePath.Text = row["image_path"].ToString() ?? "Assets/Images/placeholder.png";
            txtDispenseMessage.Text = row["dispense_message"].ToString() ?? "Enjoy your item!";
            txtExamineMessage.Text = row["examine_message"].ToString() ?? "A standard vending item.";
        }

        public InventoryItemWindow(string slotId, int itemId, int stock, decimal? slotPrice) : this()
        {
            TitleContent.Text = "Edit Machine Slot";
            txtSlotId.Text = SlotIdHelper.Normalize(slotId) ?? slotId;
            txtStock.Text = stock.ToString(CultureInfo.InvariantCulture);
            txtSlotPrice.Text = slotPrice.HasValue ? slotPrice.Value.ToString("0.00", CultureInfo.InvariantCulture) : "";

            foreach (DataRowView rowView in cboExistingItem.Items)
            {
                if (Convert.ToInt32(rowView.Row["item_id"]) == itemId)
                {
                    cboExistingItem.SelectedItem = rowView;
                    break;
                }
            }
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

            if (SelectedItemId == null)
            {
                MessageBox.Show("Please select a global item for this slot.", "Missing Item", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtStock.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stock) || stock < 0)
            {
                MessageBox.Show("Stock must be a valid non-negative integer.", "Invalid Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtSlotPrice.Text) &&
                !decimal.TryParse(txtSlotPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Machine price override must be a valid number or left blank.", "Invalid Price", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public string SlotId => SlotIdHelper.Normalize(txtSlotId.Text) ?? txtSlotId.Text.Trim();

        public int InitialStock => int.TryParse(txtStock.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int stock) ? stock : 0;

        public int MaxCapacity => 15;

        public decimal? SlotPriceOverride
        {
            get
            {
                if (string.IsNullOrWhiteSpace(txtSlotPrice.Text))
                {
                    return null;
                }

                return decimal.TryParse(txtSlotPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price)
                    ? price
                    : null;
            }
        }

        public int? SelectedItemId
        {
            get
            {
                if (cboExistingItem.SelectedItem is DataRowView rowView)
                {
                    return Convert.ToInt32(rowView.Row["item_id"]);
                }

                return null;
            }
        }
    }
}
