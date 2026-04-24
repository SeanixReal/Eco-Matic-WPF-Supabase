using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Eco_Matic
{
    public partial class EditMachineWindow : Window
    {
        public string LocationName { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public string Status { get; private set; } = string.Empty;
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }

        public EditMachineWindow(string currentLocation, string currentAddress, string currentStatus, double? currentLatitude = null, double? currentLongitude = null)
        {
            InitializeComponent();
            txtLocationName.Text = currentLocation;
            txtAddress.Text = currentAddress;
            Latitude = currentLatitude;
            Longitude = currentLongitude;
            txtCoordinates.Text = Latitude.HasValue && Longitude.HasValue
                ? $"{Latitude.Value:F5}, {Longitude.Value:F5}"
                : "Coordinates not set";

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
            Address = txtAddress.Text.Trim();
            Status = (cboStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Active";
            DialogResult = true;
            Close();
        }

        private void BtnPickOnMap_Click(object sender, RoutedEventArgs e)
        {
            var picker = new MapPickerWindow(txtAddress.Text, Latitude, Longitude)
            {
                Owner = this
            };

            if (picker.ShowDialog() == true)
            {
                Latitude = picker.SelectedLatitude;
                Longitude = picker.SelectedLongitude;
                txtAddress.Text = picker.SelectedAddress;
                txtCoordinates.Text = Latitude.HasValue && Longitude.HasValue
                    ? $"{Latitude.Value:F5}, {Longitude.Value:F5}"
                    : "Coordinates not set";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
