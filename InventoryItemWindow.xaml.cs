using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Eco_Matic
{
    public partial class InventoryItemWindow : Window
    {
        public InventoryItemWindow()
        {
            InitializeComponent();
        }

        public InventoryItemWindow(string name, string type, decimal price, int calories, int stock, int maxCap, string imagePath, string dispenseMessage = "Enjoy your item!", string examineMessage = "A standard vending item.") : this()
        {
            TitleContent.Text = "Modify Inventory Item";
            txtName.Text = name;
            txtPrice.Text = price.ToString("0.00");
            txtCalories.Text = calories.ToString();
            txtStock.Text = stock.ToString();
            txtImagePath.Text = string.IsNullOrWhiteSpace(imagePath) ? "/Assets/Placeholder.png" : imagePath;
            txtDispenseMessage.Text = string.IsNullOrWhiteSpace(dispenseMessage) ? "Enjoy your item!" : dispenseMessage;
            txtExamineMessage.Text = string.IsNullOrWhiteSpace(examineMessage) ? "A standard vending item." : examineMessage;

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
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                MessageBox.Show("Please fill out all required fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        
        public string ItemName => txtName.Text;
        public string ItemType => (cboType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Misc";
        public string ImagePath => string.IsNullOrWhiteSpace(txtImagePath.Text) ? "/Assets/Placeholder.png" : txtImagePath.Text;
        public decimal Price => decimal.Parse(txtPrice.Text);
        public int Calories => int.TryParse(txtCalories.Text, out int cal) ? cal : 0;
        public int InitialStock => int.Parse(txtStock.Text);
        public int MaxCapacity => 15;
        public string DispenseMessage => txtDispenseMessage.Text;
        public string ExamineMessage => txtExamineMessage.Text;
    }
}
