using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;

namespace Eco_Matic
{
    public partial class InventoryItemWindow : Window
    {
        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string sourceFile = openFileDialog.FileName;
                string fileName = Path.GetFileName(sourceFile);
                
                // Define the destination path in Assets/Images
                string targetDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Images");
                
                // Ensure directory exists
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string targetFile = Path.Combine(targetDir, fileName);

                try
                {
                    // If the file is not already in the target directory, copy it
                    if (Path.GetFullPath(sourceFile) != Path.GetFullPath(targetFile))
                    {
                        File.Copy(sourceFile, targetFile, true);
                    }
                    
                    // Set the relative path for the database
                    txtImagePath.Text = $"Assets/Images/{fileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error copying image: {ex.Message}", "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public InventoryItemWindow()
        {
            InitializeComponent();
            LoadCatalog();
        }

        private void LoadCatalog()
        {
            var store = new Data.MySqlStore();
            var dt = store.GetAllItems();
            
            // Add a "Create New Item" dummy row
            var row = dt.NewRow();
            row["item_id"] = DBNull.Value;
            row["name"] = "-- Create New Item --";
            dt.Rows.InsertAt(row, 0);

            cboExistingItem.ItemsSource = dt.DefaultView;
            cboExistingItem.SelectedIndex = 0;
        }

        private void CboExistingItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboExistingItem.SelectedItem is System.Data.DataRowView rowView)
            {
                var row = rowView.Row;
                if (row["item_id"] != DBNull.Value)
                {
                    // Existing item selected: populate and lock fields
                    txtName.Text = row["name"].ToString() ?? "";
                    txtPrice.Text = Convert.ToDecimal(row["price"]).ToString("0.00");
                    txtCalories.Text = row["calories"].ToString() ?? "0";
                    txtImagePath.Text = row["image_path"].ToString() ?? "Assets/Images/placeholder.png";
                    txtDispenseMessage.Text = row["dispense_message"].ToString() ?? "Enjoy your item!";
                    txtExamineMessage.Text = row["examine_message"].ToString() ?? "A standard vending item.";
                    
                    string type = row["type"].ToString() ?? "";
                    foreach (ComboBoxItem item in cboType.Items)
                    {
                        if (item.Content.ToString() == type)
                        {
                            cboType.SelectedItem = item;
                            break;
                        }
                    }

                    // Lock fields to prevent accidental global edits from this window
                    // (Unless the user specifically wants to edit the catalog record)
                    txtName.IsEnabled = false;
                    cboType.IsEnabled = false;
                    txtPrice.IsEnabled = false;
                    txtCalories.IsEnabled = false;
                    txtImagePath.IsEnabled = false;
                    txtDispenseMessage.IsEnabled = false;
                    txtExamineMessage.IsEnabled = false;
                    btnBrowseImage.IsEnabled = false;
                }
                else
                {
                    // "Create New Item" selected: unlock fields
                    txtName.IsEnabled = true;
                    cboType.IsEnabled = true;
                    txtPrice.IsEnabled = true;
                    txtCalories.IsEnabled = true;
                    txtImagePath.IsEnabled = true;
                    txtDispenseMessage.IsEnabled = true;
                    txtExamineMessage.IsEnabled = true;
                    btnBrowseImage.IsEnabled = true;

                    // Clear fields if they were previously filled by an existing item
                    if (e.RemovedItems.Count > 0)
                    {
                        txtName.Text = "";
                        txtPrice.Text = "";
                        txtCalories.Text = "";
                        txtImagePath.Text = "Assets/Images/placeholder.png";
                        txtDispenseMessage.Text = "Enjoy your item!";
                        txtExamineMessage.Text = "A standard vending item.";
                    }
                }
            }
        }

        public InventoryItemWindow(string slotId, string name, string type, decimal price, int calories, int stock, int maxCap, string imagePath, string dispenseMessage = "Enjoy your item!", string examineMessage = "A standard vending item.") : this()
        {
            TitleContent.Text = "Modify Inventory Item";
            txtSlotId.Text = slotId;
            txtName.Text = name;
            txtPrice.Text = price.ToString("0.00");
            txtCalories.Text = calories.ToString();
            txtStock.Text = stock.ToString();
            txtImagePath.Text = string.IsNullOrWhiteSpace(imagePath) ? "Assets/Images/placeholder.png" : imagePath;
            txtDispenseMessage.Text = string.IsNullOrWhiteSpace(dispenseMessage) ? "Enjoy your item!" : dispenseMessage;
            txtExamineMessage.Text = string.IsNullOrWhiteSpace(examineMessage) ? "A standard vending item." : examineMessage;
            
            // Hide catalog selection when editing an existing slot
            cboExistingItem.Visibility = Visibility.Collapsed;
            // Also hide the separator
            foreach (var child in ((StackPanel)txtSlotId.Parent).Children)
            {
                if (child is Separator) ((Separator)child).Visibility = Visibility.Collapsed;
                if (child is TextBlock tb && tb.Text.Contains("Select Existing")) tb.Visibility = Visibility.Collapsed;
            }

            foreach (ComboBoxItem item in cboType.Items)
            {
                if (item.Content.ToString() == type)
                {
                    cboType.SelectedItem = item;
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
            if (string.IsNullOrWhiteSpace(txtSlotId.Text) || string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                MessageBox.Show("Please fill out all required fields (Slot ID, Name, Price).", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(txtPrice.Text, out decimal price))
            {
                MessageBox.Show("Price must be a valid number.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(txtStock.Text, out int stock))
            {
                MessageBox.Show("Stock must be a valid integer.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        
        public string SlotId => txtSlotId.Text;
        public string ItemName => txtName.Text;
        public string ItemType => (cboType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Misc";
        public string ImagePath => string.IsNullOrWhiteSpace(txtImagePath.Text) ? "/Assets/Placeholder.png" : txtImagePath.Text;
        public decimal Price => decimal.TryParse(txtPrice.Text, out decimal p) ? p : 0m;
        public int Calories => int.TryParse(txtCalories.Text, out int cal) ? cal : 0;
        public int InitialStock => int.TryParse(txtStock.Text, out int s) ? s : 15;
        public int MaxCapacity => 15;
        public string DispenseMessage => txtDispenseMessage.Text;
        public string ExamineMessage => txtExamineMessage.Text;

        public int? SelectedItemId
        {
            get
            {
                if (cboExistingItem.SelectedItem is System.Data.DataRowView rowView)
                {
                    var row = rowView.Row;
                    if (row["item_id"] != DBNull.Value)
                        return Convert.ToInt32(row["item_id"]);
                }
                return null;
            }
        }
    }
}
