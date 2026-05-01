using System;
using System.Windows;
using System.Windows.Input;
using MessageBox = Eco_Matic.Utilities.WindowDialog;

namespace Eco_Matic
{
    public partial class AddMachineWindow : Window
    {
        public string LocationName { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }

        public AddMachineWindow()
        {
            InitializeComponent();
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
            if (string.IsNullOrWhiteSpace(txtMachineName.Text))
            {
                MessageBox.Show("Please enter a valid machine name.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LocationName = txtMachineName.Text.Trim();
            Address = txtAddress.Text.Trim();
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
