using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;


namespace Eco_Matic
{
    public partial class UserEditorWindow : Window
    {
        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public int RoleId { get; private set; }
        public int? AssignedMachineId { get; private set; }

        public UserEditorWindow()
        {
            InitializeComponent();
            btnConfirm.IsEnabled = false;
            cboRole.IsEnabled = false;
            cboMachine.IsEnabled = false;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadReferenceDataAsync();
        }

        private async Task LoadReferenceDataAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var result = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    return (Roles: store.GetRoles(), Machines: store.GetVendingMachinesLookup());
                });

                cboRole.ItemsSource = result.Roles.DefaultView;
                cboRole.DisplayMemberPath = "role_name";
                cboRole.SelectedValuePath = "role_id";

                cboMachine.ItemsSource = result.Machines.DefaultView;
                cboMachine.DisplayMemberPath = "location_name";
                cboMachine.SelectedValuePath = "machine_id";

                txtLoadStatus.Text = "Reference data loaded.";
            }
            catch (Exception ex)
            {
                txtLoadStatus.Text = "Failed to load roles or machines.";
                MessageBox.Show(this,
                    $"Could not load staff setup data.\n\n{ex.Message}",
                    "User Editor Load Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btnConfirm.IsEnabled = true;
                cboRole.IsEnabled = true;
                cboMachine.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CboRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboRole.SelectedItem is System.Data.DataRowView row)
            {
                string roleName = row["role_name"].ToString() ?? "";
                if (roleName == "Inventory Manager")
                {
                    lblMachine.Visibility = Visibility.Visible;
                    cboMachine.Visibility = Visibility.Visible;
                }
                else
                {
                    lblMachine.Visibility = Visibility.Collapsed;
                    cboMachine.Visibility = Visibility.Collapsed;
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
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Password) || cboRole.SelectedValue == null)
            {
                MessageBox.Show("Please complete all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Username = txtUsername.Text.Trim();
            Password = txtPassword.Password;
            RoleId = Convert.ToInt32(cboRole.SelectedValue);

            if (lblMachine.Visibility == Visibility.Visible)
            {
                if (cboMachine.SelectedValue == null)
                {
                    MessageBox.Show("Please assign a machine to the inventory manager.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                AssignedMachineId = Convert.ToInt32(cboMachine.SelectedValue);
            }
            else
            {
                AssignedMachineId = null;
            }

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
