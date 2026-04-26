using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Eco_Matic
{
    public partial class UserEditorWindow : Window
    {
        private readonly bool _isEditMode;
        private readonly HashSet<int> _initialMachineIds = new();

        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public int RoleId { get; private set; }
        public int? AssignedMachineId { get; private set; }
        public List<int> AssignedMachineIds { get; } = new();

        public UserEditorWindow()
        {
            InitializeComponent();
            btnConfirm.IsEnabled = false;
            lstMachines.IsEnabled = false;
        }

        public UserEditorWindow(string username, IEnumerable<int> assignedMachineIds)
            : this()
        {
            _isEditMode = true;
            txtUsername.Text = username;
            txtUsername.IsReadOnly = true;
            txtUsername.Background = System.Windows.Media.Brushes.WhiteSmoke;
            lblPassword.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Collapsed;
            btnConfirm.Content = "Save";
            foreach (int machineId in assignedMachineIds.Where(id => id > 0))
            {
                _initialMachineIds.Add(machineId);
            }
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
                    return (InventoryManagerRoleId: store.GetInventoryManagerRoleId(), Machines: store.GetVendingMachinesLookup());
                });

                if (!result.InventoryManagerRoleId.HasValue)
                {
                    throw new InvalidOperationException("The Inventory Manager role is missing in Supabase.");
                }

                RoleId = result.InventoryManagerRoleId.Value;

                lstMachines.ItemsSource = result.Machines.DefaultView;
                ApplyInitialMachineSelection();

                txtLoadStatus.Text = "Inventory managers can only manage inventory for their assigned machines.";
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
                lstMachines.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void ApplyInitialMachineSelection()
        {
            if (!_isEditMode || _initialMachineIds.Count == 0)
            {
                return;
            }

            foreach (object item in lstMachines.Items)
            {
                if (item is System.Data.DataRowView row &&
                    int.TryParse(row["machine_id"]?.ToString(), out int machineId) &&
                    _initialMachineIds.Contains(machineId))
                {
                    lstMachines.SelectedItems.Add(item);
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
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || (!_isEditMode && string.IsNullOrWhiteSpace(txtPassword.Password)) || RoleId <= 0)
            {
                MessageBox.Show("Please complete all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Username = txtUsername.Text.Trim();
            Password = txtPassword.Password;

            AssignedMachineIds.Clear();
            foreach (object selectedItem in lstMachines.SelectedItems)
            {
                if (selectedItem is System.Data.DataRowView row &&
                    int.TryParse(row["machine_id"]?.ToString(), out int machineId) &&
                    machineId > 0 &&
                    !AssignedMachineIds.Contains(machineId))
                {
                    AssignedMachineIds.Add(machineId);
                }
            }

            if (AssignedMachineIds.Count == 0)
            {
                MessageBox.Show("Please assign at least one vending machine to the inventory manager.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AssignedMachineIds.Sort();
            AssignedMachineId = AssignedMachineIds[0];

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
