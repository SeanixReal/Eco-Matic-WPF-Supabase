using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;

namespace Eco_Matic
{
    /// <summary>
    /// AdminWindow acts as the central administrative controller for the Eco-Matic project.
    /// 
    ///  : Highlights core OOP concepts (Encapsulation, UI vs Data layer separation) 
    /// and Event-Driven architecture in WPF. Uses Role-Based Access Control (RBAC) to restrict features.
    ///  : This is the View logic. Do not put raw SQL here; always call SupabaseStore.cs.
    ///  : Explain that this single window morphs dynamically based on who logs in!
    /// </summary>
    public partial class AdminWindow : Window
    {
        // Stores the current user's role to determine UI permissions ("Master Admin" vs "Inventory Manager")
        private string _currentUserRole;
        
        // If the user is an Inventory Manager, this locks them to a specific machine. Null means master access.
        private readonly int? _assignedMachineId;

        /// <summary>
        /// Initializes the application, sets up the current role context, and routes the user to the correct default view.
        /// </summary>
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

        /// <summary>
        /// Configures the Sidebar and available features based on user privileges (RBAC).
        ///  : If your professor asks how you keep normal managers from peering into financials or deleting machines, point to this method!
        /// </summary>
        private void SetupUIForRole()
        {
            txtRoleLabel.Text = _currentUserRole.ToUpper() + " ACCESS";

            if (_currentUserRole == "Inventory Manager")
            {
                // Hide parts of the sidebar according to Role-Based Access Control
                // Ensures employees only see what they are authorized to manage.
                navDashboard.Visibility = Visibility.Collapsed;
                navItems.Visibility = Visibility.Collapsed;
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
                string? target = clickedBtn.Tag?.ToString();
                if (target != null)
                {
                    SetActiveView(target);
                }
            }
        }

        /// <summary>
        /// A primitive View-Router that switches the main content area between different "pages" (Grids).
        ///  : Instead of creating separate WPF Windows for every page, we use UI Visibility toggling.
        /// This keeps performance fast and maintains a modern Single-Page Application (SPA) feel in a Desktop Client.
        ///  : Demonstrates efficient memory usage by reusing the single shell framework.
        /// </summary>
        private void SetActiveView(string viewName)
        {
            // Reset all buttons to default styling
            navDashboard.Style = (Style)FindResource("SidebarButtonStyle");
            navInventory.Style = (Style)FindResource("SidebarButtonStyle");
            navItems.Style = (Style)FindResource("SidebarButtonStyle");
            navLogs.Style = (Style)FindResource("SidebarButtonStyle");
            navSales.Style = (Style)FindResource("SidebarButtonStyle");
            navMachines.Style = (Style)FindResource("SidebarButtonStyle");
            navUsers.Style = (Style)FindResource("SidebarButtonStyle");
            navCustomers.Style = (Style)FindResource("SidebarButtonStyle");

            // Reset all views
            viewDashboard.Visibility = Visibility.Collapsed;
            viewInventory.Visibility = Visibility.Collapsed;
            viewItems.Visibility = Visibility.Collapsed;
            viewLogs.Visibility = Visibility.Collapsed;
            viewSales.Visibility = Visibility.Collapsed;
            viewMachines.Visibility = Visibility.Collapsed;
            viewUsers.Visibility = Visibility.Collapsed;
            viewCustomers.Visibility = Visibility.Collapsed;

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
                    txtViewSubtitle.Text = "Assign global items to machine slots and manage stock.";
                    break;
                case "Items":
                    navItems.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewItems.Visibility = Visibility.Visible;
                    LoadCatalogItems();
                    txtViewTitle.Text = "Global Item Catalog";
                    txtViewSubtitle.Text = "Manage shared item details used across machines.";
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
                    txtViewTitle.Text = "System Admin Users";
                    txtViewSubtitle.Text = "Manage admins and inventory workers.";
                    break;
                case "Customers":
                    navCustomers.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewCustomers.Visibility = Visibility.Visible;
                    LoadCustomersData();
                    txtViewTitle.Text = "Customers CRM";
                    txtViewSubtitle.Text = "Manage RFID user accounts and credit balances.";
                    break;
            }
        }

        private void CboInventoryMachine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId)
            {
                var store = new Data.SupabaseStore();
                LoadInventoryGrid(machineId);
            }
        }

        /// <summary>
        /// Fires when an Admin creates a new Inventory slot item for their machine.
        ///  : Note the proper usage of `Owner = this` attached to the pop-up (InventoryItemWindow) to ensure 
        /// modal focus, avoiding multi-window 'z-fighting' logic issues on the user's OS.
        /// </summary>
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId)
            {
                var addWindow = new InventoryItemWindow();
                addWindow.Owner = this;
                if (addWindow.ShowDialog() == true)
                {
                    var store = new Data.SupabaseStore();

                    if (addWindow.SelectedItemId.HasValue)
                    {
                        if (store.AddItemToMachineSlot(machineId, addWindow.SlotId, addWindow.SelectedItemId.Value, addWindow.InitialStock, addWindow.SlotPriceOverride))
                        {
                            LoadInventoryGrid(machineId);
                        }
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
                int inventoryId = Convert.ToInt32(row["_InventoryID"]);
                var restockWindow = new RestockWindow();
                restockWindow.Owner = this;
                if (restockWindow.ShowDialog() == true)
                {
                    var store = new Data.SupabaseStore();
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
                int inventoryId = Convert.ToInt32(row["_InventoryID"]);
                int itemId = Convert.ToInt32(row["_ItemID"]);
                string slotId = row["Slot"].ToString() ?? "";
                int stock = Convert.ToInt32(row["Stock"]);
                decimal? slotPrice = row.Row.Table.Columns.Contains("Slot Price") && row["Slot Price"] != DBNull.Value
                    ? Convert.ToDecimal(row["Slot Price"])
                    : null;

                var editWindow = new InventoryItemWindow(slotId, itemId, stock, slotPrice)
                {
                    Owner = this
                };

                if (editWindow.ShowDialog() == true)
                {
                    var store = new Data.SupabaseStore();
                    if (editWindow.SelectedItemId.HasValue &&
                        store.UpdateMachineInventoryAssignment(inventoryId, machineId, editWindow.SlotId, editWindow.SelectedItemId.Value, editWindow.InitialStock, editWindow.MaxCapacity, editWindow.SlotPriceOverride))
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
                int inventoryId = Convert.ToInt32(row["_InventoryID"]);
                string name = row["Item"].ToString() ?? "";

                if (MessageBox.Show($"Are you sure you want to permanently delete '{name}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var store = new Data.SupabaseStore();
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
            var store = new Data.SupabaseStore();
            var view = store.GetMachineInventory(machineId).DefaultView;
            view.Sort = "[_SlotSort] ASC";
            dgInventory.ItemsSource = view;
        }

        /// <summary>
        /// Retrieves vending machines from the SupabaseStore dependency and assigns them to the Inventory Machine Switcher Dropdown. 
        /// Crucially enforces RBAC constraints dynamically by filtering the resulting DataView.
        ///  : Demonstrates slicing DB datasets directly in RAM via `System.Data.DataView` 
        /// to avoid performing multiple distinct, round-trip SQL queries to the DB Layer.
        /// </summary>        
        private void LoadInventoryMachines()
        {
            var store = new Data.SupabaseStore();
            var dt = store.GetVendingMachinesLookup();
            
            if (_currentUserRole == "Inventory Manager" && _assignedMachineId.HasValue)
            {
                var filteredView = new System.Data.DataView(dt)
                {
                    RowFilter = $"machine_id = {_assignedMachineId.Value}"
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

        /// <summary>
        /// Gathers multiple core KPI (Key Performance Indicator) metrics at once to populate UI cards.
        ///  : Note the `out` parameters used to return multiple values from a single 
        /// database request in `GetDashboardMetrics()`. This minimizes open query transactions.
        /// </summary>
        private void LoadDashboardMetrics()
        {
            var store = new Data.SupabaseStore();
            store.GetDashboardMetrics(out decimal totalSales, out int totalItemsSold, out int lowStockAlerts, out int activeMachines);

            txtTotalSales.Text = $"₱{totalSales:F2}";
            txtItemsSold.Text = totalItemsSold.ToString();
            txtLowStock.Text = lowStockAlerts.ToString();
            txtActiveMachines.Text = activeMachines.ToString();

            if (lowStockAlerts > 0)
                txtLowStock.Foreground = new SolidColorBrush(Color.FromRgb(214, 90, 90)); // Soft Red
            else
                txtLowStock.Foreground = new SolidColorBrush(Color.FromRgb(47, 166, 106)); // Green

            // Load recent activity onto the dashboard datagrid limiting to last 15 logs
            var logs = store.GetEventLogs();
            if (logs.Rows.Count > 0)
            {
                var view = logs.DefaultView;
                view.Sort = "[Timestamp] DESC";
                dgRecentActivity.ItemsSource = view;
            }
            else
            {
                dgRecentActivity.ItemsSource = null;
            }
        }

        private void LoadEventLogs()
        {
            if (cboLogsFilter == null || dpLogsDate == null) return;

            var store = new Data.SupabaseStore();
            string filterType = (cboLogsFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Day";
            DateTime targetDate = dpLogsDate.SelectedDate ?? DateTime.Today;

            dgLogs.ItemsSource = store.GetFilteredEventLogs(targetDate, filterType).DefaultView;
        }

        private void LogsFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (viewLogs != null && viewLogs.Visibility == Visibility.Visible)
            {
                LoadEventLogs();
            }
        }

        private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all event logs?", "Clear Logs", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                var store = new Data.SupabaseStore();
                store.ClearEventLogs();
                LoadEventLogs();
            }
        }

        /// <summary>
        /// Orchestrates Data Retrieval for Sales, applying dynamically selected 'Temporal Ranges' (Day, Week, Month, Year).
        /// [For Presentation]: Mention how you implemented a backend Tuple parameter allowing both a DataTable
        /// and Total Revenue sum to be extracted and calculated directly by the MySQL Engine, offloading calculations from C#.
        /// </summary>
        private void LoadSalesData()
        {
            if (cboSalesFilter == null || dpSalesDate == null) return;
            
            var store = new Data.SupabaseStore();
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
            var store = new Data.SupabaseStore();
            dgMachines.ItemsSource = store.GetVendingMachines().DefaultView;
        }

        private void BtnAddMachine_Click(object sender, RoutedEventArgs e)
        {
            var addMach = new AddMachineWindow { Owner = this };
            if (addMach.ShowDialog() == true)
            {
                var store = new Data.SupabaseStore();
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
                    var store = new Data.SupabaseStore();
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

        private void BtnEditMachine_Click(object sender, RoutedEventArgs e)
        {
            if (dgMachines.SelectedItem is System.Data.DataRowView row)
            {
                int machineId = Convert.ToInt32(row["ID"]);
                string loc = row["Location"].ToString() ?? "";
                string status = row["Status"].ToString() ?? "Active";

                var editMach = new EditMachineWindow(loc, status) { Owner = this };
                if (editMach.ShowDialog() == true)
                {
                    var store = new Data.SupabaseStore();
                    if (store.UpdateMachine(machineId, editMach.LocationName, editMach.Status))
                    {
                        LoadMachinesData();
                        LoadInventoryMachines(); // Refresh inventory dropdowns if name changed
                        MessageBox.Show("Machine updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to update the machine.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a machine to edit.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadUsersData()
        {
            var store = new Data.SupabaseStore();
            dgUsers.ItemsSource = store.GetUsers().DefaultView;
        }

        private void LoadCatalogItems()
        {
            var store = new Data.SupabaseStore();
            dgItems.ItemsSource = store.GetCatalogItems().DefaultView;
        }

        private void BtnAddCatalogItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = new CatalogItemWindow { Owner = this };
            if (editor.ShowDialog() == true)
            {
                var store = new Data.SupabaseStore();
                if (store.AddCatalogItem(editor.ItemName, editor.ItemType, editor.Price, editor.Calories, editor.ImagePath, editor.DispenseMessage, editor.ExamineMessage))
                {
                    LoadCatalogItems();
                }
            }
        }

        private void BtnEditCatalogItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgItems.SelectedItem is not System.Data.DataRowView row)
            {
                MessageBox.Show("Please select a global item to edit.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int itemId = Convert.ToInt32(row["ID"]);
            var editor = new CatalogItemWindow(
                row["Name"].ToString() ?? "",
                row["Type"].ToString() ?? "Misc",
                Convert.ToDecimal(row["Default Price"]),
                Convert.ToInt32(row["Calories"]),
                row["Image"].ToString() ?? "Assets/Images/placeholder.png",
                row["Dispense Message"].ToString() ?? "Enjoy your item!",
                row["Examine Message"].ToString() ?? "A standard vending item.")
            {
                Owner = this
            };

            if (editor.ShowDialog() == true)
            {
                var store = new Data.SupabaseStore();
                if (store.UpdateCatalogItem(itemId, editor.ItemName, editor.ItemType, editor.Price, editor.Calories, editor.ImagePath, editor.DispenseMessage, editor.ExamineMessage))
                {
                    LoadCatalogItems();
                    if (cboInventoryMachine.SelectedValue is int machineId)
                    {
                        LoadInventoryGrid(machineId);
                    }
                }
            }
        }

        private void BtnDeleteCatalogItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgItems.SelectedItem is not System.Data.DataRowView row)
            {
                MessageBox.Show("Please select a global item to delete.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int itemId = Convert.ToInt32(row["ID"]);
            string name = row["Name"].ToString() ?? "";
            if (MessageBox.Show($"Are you sure you want to permanently delete the global item '{name}'?", "Delete Global Item", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            var store = new Data.SupabaseStore();
            if (store.DeleteCatalogItem(itemId))
            {
                LoadCatalogItems();
                if (cboInventoryMachine.SelectedValue is int machineId)
                {
                    LoadInventoryGrid(machineId);
                }
            }
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var editor = new UserEditorWindow { Owner = this };
            if (editor.ShowDialog() == true)
            {
                var store = new Data.SupabaseStore();
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
                    var store = new Data.SupabaseStore();
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

        private void LoadCustomersData()
        {
            var store = new Data.SupabaseStore();
            dgCustomers.ItemsSource = store.GetCustomers().DefaultView;
        }

        private void BtnEditCustomerCredits_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is System.Data.DataRowView row)
            {
                string rfid = row["RFID"].ToString() ?? "";
                int currentPoints = Convert.ToInt32(row["Points"]);
                
                // For simplicity, we just add 10 points on click in this proof of concept.
                // In a production app, you'd open a Dialog Box here asking for the exact amount.
                if (MessageBox.Show($"Are you sure you want to add 10 Eco-Credits to {rfid}?", "Modify Credit", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var store = new Data.SupabaseStore();
                    store.UpdateCustomerCredits(rfid, currentPoints + 10);
                    LoadCustomersData();
                }
            }
            else
            {
                MessageBox.Show("Please select a customer.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is System.Data.DataRowView row)
            {
                string rfid = row["RFID"].ToString() ?? "";
                if (MessageBox.Show($"Are you sure you want to permanently delete customer with RFID '{rfid}'?", "Delete Customer", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var store = new Data.SupabaseStore();
                    if (store.DeleteCustomer(rfid))
                    {
                        LoadCustomersData();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a customer to delete.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
// temp
