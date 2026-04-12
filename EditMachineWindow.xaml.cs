using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Eco_Matic
{
    public partial class EditMachineWindow : Window
    {
        public string LocationName { get; private set; } = string.Empty;
        public string Status { get; private set; } = string.Empty;

        public EditMachineWindow(string currentLocation, string currentStatus)
        {
            InitializeComponent();
            txtLocationName.Text = currentLocation;

            foreach (ComboBoxItem item in cboStatus.Items)
            {
                if (item.Content.ToString() == currentStatus)
                {
                    cboStatus.SelectedItem = item;
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

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLocationName.Text) || cboStatus.SelectedItem == null)
            {
                MessageBox.Show("Please complete all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LocationName = txtLocationName.Text.Trim();
            Status = (cboStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Active";
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
