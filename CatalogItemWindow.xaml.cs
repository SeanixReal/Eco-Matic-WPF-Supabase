using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace Eco_Matic
{
    public partial class CatalogItemWindow : Window
    {
        public CatalogItemWindow()
        {
            InitializeComponent();
        }

        public CatalogItemWindow(string name, string type, decimal price, int calories, string imagePath, string dispenseMessage, string examineMessage) : this()
        {
            TitleContent.Text = "Edit Catalog Item";
            txtName.Text = name;
            txtPrice.Text = price.ToString("0.00", CultureInfo.InvariantCulture);
            txtCalories.Text = calories.ToString(CultureInfo.InvariantCulture);
            txtImagePath.Text = string.IsNullOrWhiteSpace(imagePath) ? "Assets/Images/placeholder.png" : imagePath;
            txtDispenseMessage.Text = string.IsNullOrWhiteSpace(dispenseMessage) ? "Enjoy your item!" : dispenseMessage;
            txtExamineMessage.Text = string.IsNullOrWhiteSpace(examineMessage) ? "A standard vending item." : examineMessage;

            foreach (ComboBoxItem item in cboType.Items)
            {
                if (string.Equals(item.Content?.ToString(), type, StringComparison.OrdinalIgnoreCase))
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

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            string sourceFile = openFileDialog.FileName;
            string fileName = Path.GetFileName(sourceFile);
            string relativePath = $"Assets/Images/{fileName}";

            try
            {
                foreach (string targetDirectory in GetAssetTargetDirectories())
                {
                    Directory.CreateDirectory(targetDirectory);
                    File.Copy(sourceFile, Path.Combine(targetDirectory, fileName), true);
                }

                txtImagePath.Text = relativePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying image: {ex.Message}", "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string[] GetAssetTargetDirectories()
        {
            string runtimeAssets = Path.Combine(AppContext.BaseDirectory, "Assets", "Images");
            string? projectRoot = TryFindProjectRoot();

            return new[]
            {
                runtimeAssets,
                projectRoot != null ? Path.Combine(projectRoot, "Assets", "Images") : runtimeAssets
            }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        }

        private static string? TryFindProjectRoot()
        {
            string? current = AppContext.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "Eco-Matic.csproj")))
                {
                    return current;
                }

                current = Directory.GetParent(current)?.FullName;
            }

            return null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Item name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Default price must be a valid number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCalories.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("Calories must be a valid integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        public string ItemName => txtName.Text.Trim();

        public string ItemType => (cboType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Misc";

        public string ImagePath => string.IsNullOrWhiteSpace(txtImagePath.Text) ? "Assets/Images/placeholder.png" : txtImagePath.Text.Trim();

        public decimal Price => decimal.TryParse(txtPrice.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) ? price : 0m;

        public int Calories => int.TryParse(txtCalories.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int calories) ? calories : 0;

        public string DispenseMessage => string.IsNullOrWhiteSpace(txtDispenseMessage.Text) ? "Enjoy your item!" : txtDispenseMessage.Text.Trim();

        public string ExamineMessage => string.IsNullOrWhiteSpace(txtExamineMessage.Text) ? "A standard vending item." : txtExamineMessage.Text.Trim();
    }
}
