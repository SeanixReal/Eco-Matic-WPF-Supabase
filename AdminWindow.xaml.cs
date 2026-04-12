using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

namespace Eco_Matic
{
    public partial class AdminWindow : Window
    {
        private string _currentUserRole;
        private readonly int? _assignedMachineId;

        public AdminWindow(string role, int? assignedMachineId = null)
        {
            InitializeComponent();
            dpSalesDate.SelectedDate = DateTime.Today;
            _currentUserRole = role;
            _assignedMachineId = assignedMachineId;
            SetupUIForRole();
            
            // Start at the respective active view
            if (_currentUserRole == "Inventory Manager")
                SetActiveView("Inventory");
            else
                SetActiveView("Dashboard");
        }

        private void SetupUIForRole()
        {
            txtRoleLabel.Text = _currentUserRole.ToUpper() + " ACCESS";

            if (_currentUserRole == "Inventory Manager")
            {
                // Hide parts of the sidebar according to RBAC
                navDashboard.Visibility = Visibility.Collapsed;
                navLogs.Visibility = Visibility.Collapsed;
                navSales.Visibility = Visibility.Collapsed;
                navMachines.Visibility = Visibility.Collapsed;
                navUsers.Visibility = Visibility.Collapsed;
            }
        }

        private void WindowFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Navigation Sidebar Logic
        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn)
            {
                string target = clickedBtn.Tag?.ToString();
                if (target != null)
                {
                    SetActiveView(target);
                }
            }
        }

        private void SetActiveView(string viewName)
        {
            // Reset all buttons
            navDashboard.Style = (Style)FindResource("SidebarButtonStyle");
            navInventory.Style = (Style)FindResource("SidebarButtonStyle");
            navLogs.Style = (Style)FindResource("SidebarButtonStyle");
            navSales.Style = (Style)FindResource("SidebarButtonStyle");
            navMachines.Style = (Style)FindResource("SidebarButtonStyle");
            navUsers.Style = (Style)FindResource("SidebarButtonStyle");

            // Reset all views
            viewDashboard.Visibility = Visibility.Collapsed;
            viewInventory.Visibility = Visibility.Collapsed;
            viewLogs.Visibility = Visibility.Collapsed;
            viewSales.Visibility = Visibility.Collapsed;
            viewMachines.Visibility = Visibility.Collapsed;
            viewUsers.Visibility = Visibility.Collapsed;

            // Activate Target
            switch (viewName)
            {
                case "Dashboard":
                    navDashboard.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewDashboard.Visibility = Visibility.Visible;
                    LoadDashboardMetrics();
                    txtViewTitle.Text = "Dashboard";
                    txtViewSubtitle.Text = "System Overview";
                    break;
                case "Inventory":
                    navInventory.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewInventory.Visibility = Visibility.Visible;
                    LoadInventoryMachines();
                    txtViewTitle.Text = "Inventory Management";
                    txtViewSubtitle.Text = "Manage items and restock.";
                    break;
                case "Logs":
                    navLogs.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewLogs.Visibility = Visibility.Visible;
                    LoadEventLogs();
                    txtViewTitle.Text = "Event Logs";
                    txtViewSubtitle.Text = "Track system activity.";
                    break;
                case "Sales":
                    navSales.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewSales.Visibility = Visibility.Visible;
                    LoadSalesData();
                    txtViewTitle.Text = "Sales Report";
                    txtViewSubtitle.Text = "Analyze transaction history.";
                    break;
                case "Machines":
                    navMachines.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewMachines.Visibility = Visibility.Visible;
                    LoadMachinesData();
                    txtViewTitle.Text = "Vending Machines";
                    txtViewSubtitle.Text = "Manage interconnected machine instances.";
                    break;
                case "Users":
                    navUsers.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewUsers.Visibility = Visibility.Visible;
                    LoadUsersData();
                    txtViewTitle.Text = "User Manager";
                    txtViewSubtitle.Text = "Manage admins and inventory workers.";
                    break;
            }
        }

        private void CboInventoryMachine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId)
            {
                var store = new Data.MySqlStore();
                LoadInventoryGrid(machineId);
            }
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId)
            {
                var addWindow = new InventoryItemWindow();
                addWindow.Owner = this;
                if (addWindow.ShowDialog() == true)
                {
                    var store = new Data.MySqlStore();
                    if (store.AddNewItemToMachine(machineId, addWindow.ItemName, addWindow.ItemType, addWindow.Price, addWindow.Calories, addWindow.InitialStock, addWindow.MaxCapacity, addWindow.ImagePath))
                    {
                        LoadInventoryGrid(machineId);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a vending machine first.");
            }
        }

        private void BtnRestock_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId && dgInventory.SelectedItem is System.Data.DataRowView row)
            {
                int inventoryId = Convert.ToInt32(row["ID"]);
                var restockWindow = new RestockWindow();
                restockWindow.Owner = this;
                if (restockWindow.ShowDialog() == true)
                {
                    var store = new Data.MySqlStore();
                    if (store.RestockInventoryItem(inventoryId, restockWindow.RestockQuantity))
                    {
                        LoadInventoryGrid(machineId);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a vending machine and an item from the grid.");
            }
        }

        private void BtnEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId && dgInventory.SelectedItem is System.Data.DataRowView row)
            {
                int inventoryId = Convert.ToInt32(row["ID"]);
                string name = row["Item"].ToString() ?? "";
                string type = row["Type"].ToString() ?? "";
                decimal price = Convert.ToDecimal(row["Price"]);
                int calories = row["Calories"] != DBNull.Value ? Convert.ToInt32(row["Calories"]) : 0;
                string imagePath = row["Image"].ToString() ?? "";
                int stock = Convert.ToInt32(row["Stock"]);
                int maxCap = Convert.ToInt32(row["Max Capacity"]);

                var editWindow = new InventoryItemWindow(name, type, price, calories, stock, maxCap, imagePath)
                {
                    Owner = this
                };

                if (editWindow.ShowDialog() == true)
                {
                    var store = new Data.MySqlStore();
                    if (store.UpdateInventoryItem(inventoryId, editWindow.ItemName, editWindow.ItemType, editWindow.Price, editWindow.Calories, editWindow.ImagePath, editWindow.InitialStock, editWindow.MaxCapacity))
                    {
                        LoadInventoryGrid(machineId);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an item from the grid to edit.");
            }
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId && dgInventory.SelectedItem is System.Data.DataRowView row)
            {
                int inventoryId = Convert.ToInt32(row["ID"]);
                string name = row["Item"].ToString() ?? "";

                if (MessageBox.Show($"Are you sure you want to permanently delete '{name}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var store = new Data.MySqlStore();
                    if (store.DeleteInventoryItem(inventoryId))
                    {
                        LoadInventoryGrid(machineId);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an item from the grid to delete.");
            }
        }

        private void LoadInventoryGrid(int machineId)
        {
            var store = new Data.MySqlStore();
            dgInventory.ItemsSource = store.GetMachineInventory(machineId).DefaultView;
        }

        private void LoadInventoryMachines()
        {
            var store = new Data.MySqlStore();
            var dt = store.GetVendingMachines();
            
            if (_currentUserRole == "Inventory Manager" && _assignedMachineId.HasValue)
            {
                var filteredView = new System.Data.DataView(dt)
                {
                    RowFilter = $"ID = {_assignedMachineId.Value}"
                };
                cboInventoryMachine.ItemsSource = filteredView;
            }
            else
            {
                cboInventoryMachine.ItemsSource = dt.DefaultView;
            }

            if (cboInventoryMachine.Items.Count > 0)
                cboInventoryMachine.SelectedIndex = 0;
            else
                dgInventory.ItemsSource = null;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this,
                "Eco-Matic Vending Machine Admin Console\nVersion 1.0\n\nCopyright 2026 Seanix",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OpenReadmeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var readme = new ReadmeWindow
            {
                Owner = this
            };
            readme.ShowDialog();
        }

        private void LoadDashboardMetrics()
        {
            var store = new Data.MySqlStore();
            store.GetDashboardMetrics(out decimal totalSales, out int totalItemsSold, out int lowStockAlerts, out int activeMachines);

            txtTotalSales.Text = $"₱{totalSales:F2}";
            txtItemsSold.Text = totalItemsSold.ToString();
            txtLowStock.Text = lowStockAlerts.ToString();
            txtActiveMachines.Text = activeMachines.ToString();

            if (lowStockAlerts > 0)
                txtLowStock.Foreground = new SolidColorBrush(Color.FromRgb(214, 90, 90)); // Soft Red
            else
                txtLowStock.Foreground = new SolidColorBrush(Color.FromRgb(47, 166, 106)); // Green
        }

        private void LoadEventLogs()
        {
            var store = new Data.MySqlStore();
            dgLogs.ItemsSource = store.GetEventLogs().DefaultView;
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all event logs?", "Clear Logs", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                var store = new Data.MySqlStore();
                store.ClearEventLogs();
                LoadEventLogs();
            }
        }

        private void LoadSalesData()
        {
            if (cboSalesFilter == null || dpSalesDate == null) return;
            
            var store = new Data.MySqlStore();
            string filterType = (cboSalesFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Day";
            DateTime targetDate = dpSalesDate.SelectedDate ?? DateTime.Today;

            var result = store.GetFilteredSales(targetDate, filterType);
            dgSales.ItemsSource = result.Data.DefaultView;
            
            if (txtSalesFilterLabel != null) 
                txtSalesFilterLabel.Text = $"Sales ({filterType})";
                
            if (txtSalesTotal != null)
                txtSalesTotal.Text = $"₱ {result.Total:0.00}";
        }

        private void SalesFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (viewSales != null && viewSales.Visibility == Visibility.Visible)
            {
                LoadSalesData();
            }
        }

        private void LoadMachinesData()
        {
            var store = new Data.MySqlStore();
            dgMachines.ItemsSource = store.GetVendingMachines().DefaultView;
        }

        private void BtnAddMachine_Click(object sender, RoutedEventArgs e)
        {
            var addMach = new AddMachineWindow { Owner = this };
            if (addMach.ShowDialog() == true)
            {
                var store = new Data.MySqlStore();
                if (store.AddMachine(addMach.LocationName))
                {
                    LoadMachinesData();
                    LoadInventoryMachines(); // refresh dropdowns
                }
            }
        }

        private void BtnDeleteMachine_Click(object sender, RoutedEventArgs e)
        {
            if (dgMachines.SelectedItem is System.Data.DataRowView row)
            {
                int machineId = Convert.ToInt32(row["ID"]);
                string loc = row["Location"].ToString() ?? "";
                if (MessageBox.Show($"Are you sure you want to delete Machine {machineId} at '{loc}'? This removes its inventory and sales history.", "Delete Machine", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var store = new Data.MySqlStore();
                    if (store.DeleteMachine(machineId))
                    {
                        LoadMachinesData();
                        LoadInventoryMachines();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a machine to delete.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadUsersData()
        {
            var store = new Data.MySqlStore();
            dgUsers.ItemsSource = store.GetUsers().DefaultView;
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var editor = new UserEditorWindow { Owner = this };
            if (editor.ShowDialog() == true)
            {
                var store = new Data.MySqlStore();
                if (store.AddUser(editor.Username, editor.Password, editor.RoleId, editor.AssignedMachineId))
                {
                    LoadUsersData();
                }
                else
                {
                    MessageBox.Show("Could not add user. Username may already exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is System.Data.DataRowView row)
            {
                int userId = Convert.ToInt32(row["ID"]);
                string user = row["Username"].ToString() ?? "";
                if (user.ToLower() == "admin")
                {
                    MessageBox.Show("Cannot delete the master admin account.", "Restricted", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to delete user '{user}'?", "Delete User", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var store = new Data.MySqlStore();
                    if (store.DeleteUser(userId))
                    {
                        LoadUsersData();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a user to delete.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
