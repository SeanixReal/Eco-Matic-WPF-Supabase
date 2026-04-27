using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Data;
using System.Windows.Shapes;

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
        
        // If the user is an Inventory Manager, this locks them to assigned machines. Empty means master access.
        private readonly HashSet<int> _assignedMachineIds;
        private readonly string _initialViewName;
        private int _inventoryGridLoadVersion;
        private static readonly Brush[] ChartPalette =
        [
            CreateBrush(46, 119, 230),
            CreateBrush(47, 166, 106),
            CreateBrush(255, 206, 74),
            CreateBrush(214, 90, 90),
            CreateBrush(126, 87, 194),
            CreateBrush(20, 184, 166)
        ];

        private sealed class ChartDatum
        {
            public string Label { get; init; } = string.Empty;
            public string ValueText { get; init; } = string.Empty;
            public double BarWidth { get; init; }
            public decimal Value { get; init; }
            public Brush Fill { get; init; } = Brushes.SteelBlue;
        }

        /// <summary>
        /// Initializes the application, sets up the current role context, and routes the user to the correct default view.
        /// </summary>
        public AdminWindow(string role, IEnumerable<int>? assignedMachineIds = null)
        {
            InitializeComponent();
            dpSalesDate.SelectedDate = DateTime.Today;
            _currentUserRole = role;
            _assignedMachineIds = assignedMachineIds?.Where(id => id > 0).ToHashSet() ?? new HashSet<int>();
            SetupUIForRole();
            
            // Start at the respective active view
            if (_currentUserRole == "Inventory Manager")
            {
                _initialViewName = "Inventory";
            }
            else
            {
                _initialViewName = "Dashboard";
            }

            Loaded += AdminWindow_Loaded;
        }

        private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AdminWindow_Loaded;
            _ = LoadInitialViewAsync();
        }

        private async Task LoadInitialViewAsync()
        {
            try
            {
                await Task.Yield();
                await SetActiveViewAsync(_initialViewName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"The login succeeded, but the first staff view could not finish loading.\n\n{ex.Message}",
                    "Staff View Load Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
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
                navCustomers.Visibility = Visibility.Collapsed;
            }
        }

        private void WindowFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Navigation Sidebar Logic
        private async void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn)
            {
                string? target = clickedBtn.Tag?.ToString();
                if (target != null)
                {
                    await SetActiveViewAsync(target);
                }
            }
        }

        /// <summary>
        /// A primitive View-Router that switches the main content area between different "pages" (Grids).
        ///  : Instead of creating separate WPF Windows for every page, we use UI Visibility toggling.
        /// This keeps performance fast and maintains a modern Single-Page Application (SPA) feel in a Desktop Client.
        ///  : Demonstrates efficient memory usage by reusing the single shell framework.
        /// </summary>
        private async Task SetActiveViewAsync(string viewName)
        {
            if (_currentUserRole == "Inventory Manager" && viewName != "Inventory")
            {
                viewName = "Inventory";
            }

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
                    txtViewTitle.Text = "Dashboard";
                    await LoadDashboardMetricsAsync();
                    break;
                case "Inventory":
                    navInventory.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewInventory.Visibility = Visibility.Visible;
                    txtViewTitle.Text = "Inventory Management";
                    await LoadInventoryMachinesAsync();
                    break;
                case "Items":
                    navItems.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewItems.Visibility = Visibility.Visible;
                    txtViewTitle.Text = "Catalog Management";
                    await Task.WhenAll(LoadCatalogItemsAsync(), LoadRecycleCatalogAsync());
                    break;
                case "Logs":
                    navLogs.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewLogs.Visibility = Visibility.Visible;
                    txtViewTitle.Text = "Event Logs";
                    await LoadEventLogsAsync();
                    break;
                case "Sales":
                    navSales.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewSales.Visibility = Visibility.Visible;
                    txtViewTitle.Text = "Sales Report";
                    await LoadSalesMachineFilterAsync();
                    await LoadSalesDataAsync();
                    break;
                case "Machines":
                    navMachines.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewMachines.Visibility = Visibility.Visible;
                    txtViewTitle.Text = "Vending Machines";
                    await LoadMachinesDataAsync();
                    break;
                case "Users":
                    navUsers.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewUsers.Visibility = Visibility.Visible;
                    txtViewTitle.Text = "System Admin Users";
                    await LoadUsersDataAsync();
                    break;
                case "Customers":
                    navCustomers.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    viewCustomers.Visibility = Visibility.Visible;
                    txtViewTitle.Text = "Customers CRM";
                    await LoadCustomersDataAsync();
                    break;
            }
        }

        private async void CboInventoryMachine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId)
            {
                await LoadInventoryGridAsync(machineId);
            }
        }

        private async Task<bool> RunStoreMutationAsync(Func<bool> mutation, string actionName)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                return await Task.Run(mutation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"{actionName} failed.\n\n{ex.Message}",
                    "Operation Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task<T?> RunStoreOperationAsync<T>(Func<T> operation, string actionName)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                return await Task.Run(operation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"{actionName} failed.\n\n{ex.Message}",
                    "Operation Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return default;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task RefreshSelectedInventoryAsync()
        {
            if (cboInventoryMachine.SelectedValue is int machineId)
            {
                await LoadInventoryGridAsync(machineId);
            }
        }

        private async Task RefreshMachinesAndInventoryAsync()
        {
            await LoadMachinesDataAsync();
            await LoadInventoryMachinesAsync();
        }

        private async Task RefreshCatalogAndSelectedInventoryAsync()
        {
            await LoadCatalogItemsAsync();
            await RefreshSelectedInventoryAsync();
        }

        private async Task<int> GetGlobalItemCountAsync()
        {
            int? count = await RunStoreOperationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.GetAllItems().Rows.Count;
            }, "Load global item catalog");

            return count ?? 0;
        }

        private async Task<string?> GetNextAvailableSlotIdAsync(int machineId)
        {
            return await RunStoreOperationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.GetNextAvailableSlotId(machineId);
            }, "Load next available slot");
        }

        private async Task<int> GetAssignedSlotCountAsync(int machineId)
        {
            int? count = await RunStoreOperationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.GetAssignedSlotCount(machineId);
            }, "Load assigned slot count");

            return count ?? 0;
        }

        private async Task<bool> RollbackIncompleteMachineSetupAsync(int machineId, string locationName)
        {
            bool deleted = await RunStoreMutationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.DeleteMachine(machineId);
            }, "Rollback incomplete machine setup");

            await RefreshMachinesAndInventoryAsync();

            if (!deleted)
            {
                MessageBox.Show(this,
                    $"The new machine at '{locationName}' could not be rolled back automatically. Please remove it manually.",
                    "Rollback Needed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return deleted;
        }

        private async Task<bool> RunRequiredMachineSetupAsync(int machineId, string locationName)
        {
            int assignedSlots = await GetAssignedSlotCountAsync(machineId);

            while (assignedSlots < DataStore.MaxItemSlots)
            {
                if (assignedSlots >= 5)
                {
                    var addMoreChoice = MessageBox.Show(this,
                        $"Machine '{locationName}' now has {assignedSlots} assigned slots.\n\nDo you want to add another slot now?",
                        "Continue Slot Setup",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (addMoreChoice != MessageBoxResult.Yes)
                    {
                        break;
                    }
                }

                string suggestedSlotId = await GetNextAvailableSlotIdAsync(machineId) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(suggestedSlotId))
                {
                    break;
                }

                var setupWindow = new InventoryItemWindow(suggestedSlotId, assignedSlots, 5)
                {
                    Owner = this
                };

                if (setupWindow.ShowDialog() != true)
                {
                    if (assignedSlots < 5)
                    {
                        await RollbackIncompleteMachineSetupAsync(machineId, locationName);
                        MessageBox.Show(this,
                            "A new vending machine must be configured with at least 5 assigned slots. The incomplete machine setup was canceled.",
                            "Machine Setup Incomplete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return false;
                    }

                    break;
                }

                int? selectedItemId = setupWindow.SelectedItemId;
                string slotId = setupWindow.SlotId;
                int initialStock = setupWindow.InitialStock;
                decimal? slotPriceOverride = setupWindow.SlotPriceOverride;

                if (!selectedItemId.HasValue)
                {
                    continue;
                }

                bool added = await RunStoreMutationAsync(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.AddItemToMachineSlot(machineId, slotId, selectedItemId.Value, initialStock, slotPriceOverride);
                }, "Assign setup slot");

                if (!added)
                {
                    continue;
                }

                assignedSlots++;
            }

            if (assignedSlots < 5)
            {
                await RollbackIncompleteMachineSetupAsync(machineId, locationName);
                MessageBox.Show(this,
                    "The new machine was removed because it did not reach the minimum 5 assigned slots.",
                    "Machine Setup Incomplete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Fires when an Admin creates a new Inventory slot item for their machine.
        ///  : Note the proper usage of `Owner = this` attached to the pop-up (InventoryItemWindow) to ensure 
        /// modal focus, avoiding multi-window 'z-fighting' logic issues on the user's OS.
        /// </summary>
        private async void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId)
            {
                var addWindow = new InventoryItemWindow();
                addWindow.Owner = this;
                if (addWindow.ShowDialog() == true)
                {
                    int? selectedItemId = addWindow.SelectedItemId;
                    string slotId = addWindow.SlotId;
                    int initialStock = addWindow.InitialStock;
                    decimal? slotPriceOverride = addWindow.SlotPriceOverride;

                    if (selectedItemId.HasValue)
                    {
                        bool added = await RunStoreMutationAsync(() =>
                        {
                            var store = new Data.SupabaseStore();
                            return store.AddItemToMachineSlot(machineId, slotId, selectedItemId.Value, initialStock, slotPriceOverride);
                        }, "Assign item to machine slot");

                        if (added)
                        {
                            await LoadInventoryGridAsync(machineId);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a vending machine first.");
            }
        }

        private async void BtnRestock_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId && dgInventory.SelectedItem is System.Data.DataRowView row)
            {
                int inventoryId = Convert.ToInt32(row["_InventoryID"]);
                var restockWindow = new RestockWindow();
                restockWindow.Owner = this;
                if (restockWindow.ShowDialog() == true)
                {
                    int restockQuantity = restockWindow.RestockQuantity;
                    bool restocked = await RunStoreMutationAsync(() =>
                    {
                        var store = new Data.SupabaseStore();
                        return store.RestockInventoryItem(inventoryId, restockQuantity);
                    }, "Restock inventory item");

                    if (restocked)
                    {
                        await LoadInventoryGridAsync(machineId);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a vending machine and an item from the grid.");
            }
        }

        private async void BtnEditItem_Click(object sender, RoutedEventArgs e)
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
                    int? selectedItemId = editWindow.SelectedItemId;
                    string updatedSlotId = editWindow.SlotId;
                    int updatedStock = editWindow.InitialStock;
                    int maxCapacity = editWindow.MaxCapacity;
                    decimal? updatedSlotPrice = editWindow.SlotPriceOverride;

                    if (selectedItemId.HasValue)
                    {
                        bool updated = await RunStoreMutationAsync(() =>
                        {
                            var store = new Data.SupabaseStore();
                            return store.UpdateMachineInventoryAssignment(inventoryId, machineId, updatedSlotId, selectedItemId.Value, updatedStock, maxCapacity, updatedSlotPrice);
                        }, "Update machine slot");

                        if (updated)
                        {
                            await LoadInventoryGridAsync(machineId);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an item from the grid to edit.");
            }
        }

        private async void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is int machineId && dgInventory.SelectedItem is System.Data.DataRowView row)
            {
                int inventoryId = Convert.ToInt32(row["_InventoryID"]);
                string name = row["Item"].ToString() ?? "";

                if (MessageBox.Show($"Are you sure you want to permanently delete '{name}'?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    bool deleted = await RunStoreMutationAsync(() =>
                    {
                        var store = new Data.SupabaseStore();
                        return store.DeleteInventoryItem(inventoryId);
                    }, "Delete machine slot");

                    if (deleted)
                    {
                        await LoadInventoryGridAsync(machineId);
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

        private async Task LoadInventoryGridAsync(int machineId)
        {
            int loadVersion = System.Threading.Interlocked.Increment(ref _inventoryGridLoadVersion);
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataView view = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    var inventoryView = store.GetMachineInventory(machineId).DefaultView;
                    inventoryView.Sort = "[_SlotSort] ASC";
                    return inventoryView;
                });

                if (loadVersion != _inventoryGridLoadVersion)
                {
                    return;
                }

                dgInventory.ItemsSource = view;
            }
            finally
            {
                if (loadVersion == _inventoryGridLoadVersion)
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private async void BtnRestockSelectedToMax_Click(object sender, RoutedEventArgs e)
        {
            if (cboInventoryMachine.SelectedValue is not int machineId || dgInventory.SelectedItem is not System.Data.DataRowView row)
            {
                MessageBox.Show("Please select a vending machine and an item from the grid.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int inventoryId = Convert.ToInt32(row["_InventoryID"]);
            string itemName = row["Item"].ToString() ?? "this item";

            if (MessageBox.Show(
                    $"Restock '{itemName}' to max capacity?",
                    "Restock Selected to Max",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            bool restocked = await RunStoreMutationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.RestockInventoryItemToMax(inventoryId);
            }, "Restock selected slot to max");

            if (restocked)
            {
                await LoadInventoryGridAsync(machineId);
            }
            else
            {
                MessageBox.Show("Failed to restock selected item to max capacity.", "Restock Selected to Max", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            
            if (_currentUserRole == "Inventory Manager" && _assignedMachineIds.Count > 0)
            {
                var filteredView = new System.Data.DataView(dt)
                {
                    RowFilter = BuildAssignedMachineRowFilter()
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

        private async Task LoadInventoryMachinesAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataTable dt = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.GetVendingMachinesLookup();
                });

                if (_currentUserRole == "Inventory Manager" && _assignedMachineIds.Count > 0)
                {
                    var filteredView = new DataView(dt)
                    {
                        RowFilter = BuildAssignedMachineRowFilter()
                    };
                    cboInventoryMachine.ItemsSource = filteredView;
                }
                else
                {
                    cboInventoryMachine.ItemsSource = dt.DefaultView;
                }

                if (cboInventoryMachine.Items.Count > 0)
                {
                    cboInventoryMachine.SelectedIndex = 0;
                }
                else
                {
                    dgInventory.ItemsSource = null;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private string BuildAssignedMachineRowFilter()
        {
            if (_assignedMachineIds.Count == 0)
            {
                return "machine_id = -1";
            }

            return $"machine_id IN ({string.Join(",", _assignedMachineIds.OrderBy(id => id))})";
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
            var about = new AboutWindow
            {
                Owner = this
            };
            about.ShowDialog();
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

        private async Task LoadDashboardMetricsAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var result = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    store.GetDashboardMetrics(out decimal totalSales, out int totalItemsSold, out int lowStockAlerts, out int activeMachines);
                    var logs = store.GetEventLogs();
                    var yearlySales = store.GetFilteredSales(DateTime.Today, "Year").Data;
                    var stockMonitoring = store.GetStockMonitoring();
                    return (totalSales, totalItemsSold, lowStockAlerts, activeMachines, logs, yearlySales, stockMonitoring);
                });

                txtTotalSales.Text = $"₱{result.totalSales:F2}";
                txtItemsSold.Text = result.totalItemsSold.ToString();
                txtLowStock.Text = result.lowStockAlerts.ToString();
                txtActiveMachines.Text = result.activeMachines.ToString();
                txtLowStock.Foreground = result.lowStockAlerts > 0
                    ? new SolidColorBrush(Color.FromRgb(214, 90, 90))
                    : new SolidColorBrush(Color.FromRgb(47, 166, 106));

                if (result.logs.Rows.Count > 0)
                {
                    var view = result.logs.DefaultView;
                    view.Sort = "[Timestamp] DESC";
                    dgRecentActivity.ItemsSource = view;
                }
                else
                {
                    dgRecentActivity.ItemsSource = null;
                }

                icDashboardSalesTrend.ItemsSource = BuildTrendData(result.yearlySales, 280);

                DataView stockView = result.stockMonitoring.DefaultView;
                stockView.RowFilter = "[Status] = 'OUT OF STOCK' OR [Status] = 'LOW STOCK' OR [Status] = 'WATCH'";
                stockView.Sort = "[Machine] ASC, [Stock] ASC, [Item] ASC";
                dgDashboardLowStock.ItemsSource = stockView;
            }
            finally
            {
                Mouse.OverrideCursor = null;
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

        private async Task LoadEventLogsAsync()
        {
            if (cboLogsFilter == null || dpLogsDate == null)
            {
                return;
            }

            string filterType = (cboLogsFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Day";
            DateTime targetDate = dpLogsDate.SelectedDate ?? DateTime.Today;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataView view = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.GetFilteredEventLogs(targetDate, filterType).DefaultView;
                });

                dgLogs.ItemsSource = view;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void LogsFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (viewLogs != null && viewLogs.Visibility == Visibility.Visible)
            {
                await LoadEventLogsAsync();
            }
        }

        private async void BtnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all event logs?", "Clear Logs", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                bool cleared = await RunStoreMutationAsync(() =>
                {
                    var store = new Data.SupabaseStore();
                    store.ClearEventLogs();
                    return true;
                }, "Clear event logs");

                if (cleared)
                {
                    await LoadEventLogsAsync();
                }
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
            int? machineId = GetSelectedSalesMachineId();
            UpdateSalesDatePickerState(filterType);

            var result = store.GetFilteredSales(targetDate, filterType, machineId);
            dgSales.ItemsSource = result.Data.DefaultView;
            UpdateSalesReportVisuals(result.Data, result.Total, filterType);
            
            if (txtSalesFilterLabel != null) 
                txtSalesFilterLabel.Text = $"Sales ({filterType})";
                
            if (txtSalesTotal != null)
                txtSalesTotal.Text = $"₱ {result.Total:0.00}";
        }

        private async Task LoadSalesDataAsync()
        {
            if (cboSalesFilter == null || dpSalesDate == null)
            {
                return;
            }

            string filterType = (cboSalesFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Day";
            DateTime targetDate = dpSalesDate.SelectedDate ?? DateTime.Today;
            int? machineId = GetSelectedSalesMachineId();
            UpdateSalesDatePickerState(filterType);

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var result = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.GetFilteredSales(targetDate, filterType, machineId);
                });

                dgSales.ItemsSource = result.Data.DefaultView;
                UpdateSalesReportVisuals(result.Data, result.Total, filterType);

                if (txtSalesFilterLabel != null)
                    txtSalesFilterLabel.Text = $"Sales ({filterType})";

                if (txtSalesTotal != null)
                    txtSalesTotal.Text = $"₱ {result.Total:0.00}";
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task LoadSalesMachineFilterAsync()
        {
            if (cboSalesMachine == null)
            {
                return;
            }

            int? previousMachineId = GetSelectedSalesMachineId();
            DataTable machines = await Task.Run(() =>
            {
                var store = new Data.SupabaseStore();
                return store.GetVendingMachinesLookup();
            });

            DataTable filterTable = machines.Clone();
            filterTable.Rows.Add(0, "All Machines", string.Empty, DBNull.Value, DBNull.Value, string.Empty);
            foreach (DataRow row in machines.Rows)
            {
                filterTable.ImportRow(row);
            }

            cboSalesMachine.ItemsSource = filterTable.DefaultView;
            cboSalesMachine.SelectedValue = previousMachineId ?? 0;
            if (cboSalesMachine.SelectedIndex < 0)
            {
                cboSalesMachine.SelectedIndex = 0;
            }
        }

        private int? GetSelectedSalesMachineId()
        {
            if (cboSalesMachine?.SelectedValue == null)
            {
                return null;
            }

            if (int.TryParse(cboSalesMachine.SelectedValue.ToString(), out int machineId) && machineId > 0)
            {
                return machineId;
            }

            return null;
        }

        private void UpdateSalesDatePickerState(string filterType)
        {
            if (dpSalesDate == null)
            {
                return;
            }

            bool usesDate = !string.Equals(filterType, "All Time", StringComparison.OrdinalIgnoreCase);
            dpSalesDate.IsEnabled = usesDate;
            dpSalesDate.Opacity = usesDate ? 1.0 : 0.45;
        }

        private void UpdateSalesReportVisuals(DataTable salesTable, decimal total, string filterType)
        {
            int transactions = salesTable.Rows.Count;
            int itemsSold = salesTable.Rows.Cast<DataRow>().Sum(row => Convert.ToInt32(row["Quantity"]));
            decimal averageSale = transactions > 0 ? total / transactions : 0m;

            var itemGroups = salesTable.Rows.Cast<DataRow>()
                .GroupBy(row => row["Item"]?.ToString() ?? "Unknown")
                .Select(group => new
                {
                    Label = group.Key,
                    Revenue = group.Sum(row => Convert.ToDecimal(row["Total Paid"])),
                    Quantity = group.Sum(row => Convert.ToInt32(row["Quantity"]))
                })
                .OrderByDescending(group => group.Revenue)
                .ToList();

            var machineGroups = salesTable.Rows.Cast<DataRow>()
                .GroupBy(row => row["Machine"]?.ToString() ?? "Machine")
                .Select(group => new
                {
                    Label = group.Key,
                    Revenue = group.Sum(row => Convert.ToDecimal(row["Total Paid"])),
                    Quantity = group.Sum(row => Convert.ToInt32(row["Quantity"]))
                })
                .OrderByDescending(group => group.Revenue)
                .ToList();

            txtSalesTransactions.Text = transactions.ToString();
            txtSalesAverage.Text = $"₱ {averageSale:0.00}";
            txtSalesBestItem.Text = itemGroups.Count > 0 ? $"{itemGroups[0].Label} ({itemGroups[0].Quantity})" : "-";

            icSalesTrend.ItemsSource = BuildTrendData(salesTable, 390);
            icTopItems.ItemsSource = BuildGroupBars(itemGroups.Select(x => (x.Label, x.Revenue, $"₱ {x.Revenue:0.00} / {x.Quantity} sold")), 320, 5, ChartPalette[1]);
            icMachineRevenue.ItemsSource = BuildGroupBars(machineGroups.Select(x => (x.Label, x.Revenue, $"₱ {x.Revenue:0.00}")), 300, 5, ChartPalette[2]);

            var pieData = BuildPieData(itemGroups.Select(x => (x.Label, x.Revenue, $"₱ {x.Revenue:0.00}")), 5);
            icSalesPieLegend.ItemsSource = pieData;
            DrawPieChart(canvasSalesPie, pieData);
        }

        private static List<ChartDatum> BuildTrendData(DataTable salesTable, double maxBarWidth)
        {
            var groups = salesTable.Rows.Cast<DataRow>()
                .GroupBy(row => row["Period"]?.ToString() ?? "")
                .Select(group => new
                {
                    Label = string.IsNullOrWhiteSpace(group.Key) ? "No period" : group.Key,
                    FirstDate = group.Min(row => Convert.ToDateTime(row["Date"])),
                    Revenue = group.Sum(row => Convert.ToDecimal(row["Total Paid"]))
                })
                .OrderBy(group => group.FirstDate)
                .ToList();

            if (groups.Count == 0)
            {
                return [new ChartDatum { Label = "No sales", ValueText = "₱ 0.00", BarWidth = 0, Value = 0, Fill = ChartPalette[0] }];
            }

            decimal maxValue = Math.Max(1m, groups.Max(group => group.Revenue));
            return groups.Select((group, index) => new ChartDatum
            {
                Label = group.Label,
                Value = group.Revenue,
                ValueText = $"₱ {group.Revenue:0.00}",
                BarWidth = CalculateBarWidth(group.Revenue, maxValue, maxBarWidth),
                Fill = ChartPalette[index % ChartPalette.Length]
            }).ToList();
        }

        private static List<ChartDatum> BuildGroupBars(IEnumerable<(string Label, decimal Value, string ValueText)> source, double maxBarWidth, int limit, Brush fill)
        {
            var groups = source
                .Where(item => item.Value > 0)
                .Take(limit)
                .ToList();

            if (groups.Count == 0)
            {
                return [new ChartDatum { Label = "No data", ValueText = "₱ 0.00", BarWidth = 0, Value = 0, Fill = fill }];
            }

            decimal maxValue = Math.Max(1m, groups.Max(item => item.Value));
            return groups.Select(item => new ChartDatum
            {
                Label = item.Label,
                Value = item.Value,
                ValueText = item.ValueText,
                BarWidth = CalculateBarWidth(item.Value, maxValue, maxBarWidth),
                Fill = fill
            }).ToList();
        }

        private static List<ChartDatum> BuildPieData(IEnumerable<(string Label, decimal Value, string ValueText)> source, int limit)
        {
            var groups = source
                .Where(item => item.Value > 0)
                .Take(limit)
                .ToList();

            if (groups.Count == 0)
            {
                return [new ChartDatum { Label = "No sales", ValueText = "₱ 0.00", Value = 1, Fill = CreateBrush(226, 232, 240) }];
            }

            return groups.Select((item, index) => new ChartDatum
            {
                Label = item.Label,
                Value = item.Value,
                ValueText = item.ValueText,
                Fill = ChartPalette[index % ChartPalette.Length]
            }).ToList();
        }

        private static double CalculateBarWidth(decimal value, decimal maxValue, double maxBarWidth)
        {
            if (maxValue <= 0 || value <= 0)
            {
                return 0;
            }

            return Math.Max(8, (double)(value / maxValue) * maxBarWidth);
        }

        private static void DrawPieChart(Canvas canvas, IReadOnlyList<ChartDatum> data)
        {
            canvas.Children.Clear();
            if (data.Count == 0)
            {
                return;
            }

            double width = canvas.Width;
            double height = canvas.Height;
            double radius = Math.Min(width, height) / 2d - 4d;
            Point center = new(width / 2d, height / 2d);
            decimal total = Math.Max(1m, data.Sum(item => item.Value));
            double currentAngle = -90d;

            foreach (ChartDatum item in data)
            {
                double sweepAngle = (double)(item.Value / total) * 360d;
                bool isLargeArc = sweepAngle > 180d;
                Point start = PointOnCircle(center, radius, currentAngle);
                Point end = PointOnCircle(center, radius, currentAngle + sweepAngle);

                var figure = new PathFigure { StartPoint = center, IsClosed = true };
                figure.Segments.Add(new LineSegment(start, true));
                figure.Segments.Add(new ArcSegment(end, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true));
                figure.Segments.Add(new LineSegment(center, true));

                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                canvas.Children.Add(new Path
                {
                    Data = geometry,
                    Fill = item.Fill,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                });

                currentAngle += sweepAngle;
            }
        }

        private static Point PointOnCircle(Point center, double radius, double angleDegrees)
        {
            double angleRadians = angleDegrees * Math.PI / 180d;
            return new Point(
                center.X + radius * Math.Cos(angleRadians),
                center.Y + radius * Math.Sin(angleRadians));
        }

        private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private async void SalesFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (viewSales != null && viewSales.Visibility == Visibility.Visible)
            {
                await LoadSalesDataAsync();
            }
        }

        private void LoadMachinesData()
        {
            var store = new Data.SupabaseStore();
            DataTable dt = store.GetVendingMachines();
            dgMachines.ItemsSource = dt.DefaultView;
            UpdateMachineScopeUi(dt.Rows.Count);
        }

        private void DgMachines_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName.StartsWith("_", StringComparison.Ordinal))
            {
                e.Cancel = true;
            }
        }

        private async Task LoadMachinesDataAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataTable dt = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.GetVendingMachines();
                });
                var view = dt.DefaultView;
                view.Sort = "[ID] ASC";
                dgMachines.ItemsSource = view;
                UpdateMachineScopeUi(dt.Rows.Count);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnAddMachine_Click(object sender, RoutedEventArgs e)
        {
            int existingMachineCount = await Task.Run(() =>
            {
                var store = new Data.SupabaseStore();
                return store.GetVendingMachinesLookup().Rows.Count;
            });

            if (existingMachineCount >= 4)
            {
                MessageBox.Show(this,
                    "The current project scope allows a maximum of 4 vending machines.",
                    "Machine Limit Reached",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                UpdateMachineScopeUi(existingMachineCount);
                return;
            }

            int globalItemCount = await GetGlobalItemCountAsync();
            if (globalItemCount == 0)
            {
                MessageBox.Show(this,
                    "Add at least one global item first. A new vending machine now requires at least 5 assigned slots during setup.",
                    "Global Items Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                await SetActiveViewAsync("Items");
                return;
            }

            var addMach = new AddMachineWindow { Owner = this };
            if (addMach.ShowDialog() == true)
            {
                string locationName = addMach.LocationName;
                string address = addMach.Address;
                double? latitude = addMach.Latitude;
                double? longitude = addMach.Longitude;

                int? machineId = await RunStoreOperationAsync(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.CreateMachine(locationName, address, latitude, longitude);
                }, "Create vending machine");

                if (machineId.HasValue)
                {
                    bool setupCompleted = await RunRequiredMachineSetupAsync(machineId.Value, locationName);
                    await RefreshMachinesAndInventoryAsync();

                    if (!setupCompleted)
                    {
                        return;
                    }

                    await SetActiveViewAsync("Inventory");
                    cboInventoryMachine.SelectedValue = machineId.Value;
                }
            }
        }

        private void UpdateMachineScopeUi(int machineCount)
        {
            if (txtMachineScope != null)
            {
                txtMachineScope.Text = $"{machineCount} / 4 machines in use";
                txtMachineScope.Foreground = machineCount >= 4
                    ? new SolidColorBrush(Color.FromRgb(214, 90, 90))
                    : new SolidColorBrush(Color.FromRgb(71, 85, 105));
            }

            if (btnAddMachine != null)
            {
                bool canAddMachine = machineCount < 4;
                btnAddMachine.IsEnabled = canAddMachine;
                btnAddMachine.Opacity = canAddMachine ? 1.0 : 0.6;
                btnAddMachine.ToolTip = canAddMachine
                    ? "Register a new vending machine"
                    : "Maximum of 4 vending machines reached for the current project scope";
            }
        }

        private async Task PromptToAddGlobalItemsAsync()
        {
            bool hasGlobalItems = await Task.Run(() =>
            {
                var store = new Data.SupabaseStore();
                return store.GetAllItems().Rows.Count > 0;
            });

            if (hasGlobalItems)
            {
                return;
            }

            var choice = MessageBox.Show(this,
                "This vending machine was created, but the global item catalog is still empty.\n\nAdd global items now so you can assign them to machine slots next.",
                "Add Global Items",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (choice == MessageBoxResult.Yes)
            {
                await SetActiveViewAsync("Items");
            }
        }

        private async void BtnDeleteMachine_Click(object sender, RoutedEventArgs e)
        {
            if (dgMachines.SelectedItem is System.Data.DataRowView row)
            {
                int machineId = Convert.ToInt32(row["ID"]);
                string machineName = row["Name"].ToString() ?? "";
                if (MessageBox.Show($"Are you sure you want to delete Machine {machineId} '{machineName}'? This removes its inventory and sales history.", "Delete Machine", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    bool deleted = await RunStoreMutationAsync(() =>
                    {
                        var store = new Data.SupabaseStore();
                        return store.DeleteMachine(machineId);
                    }, "Delete vending machine");

                    if (deleted)
                    {
                        await RefreshMachinesAndInventoryAsync();
                    }
                    else
                    {
                        MessageBox.Show(this, "Failed to delete the machine.", "Delete Machine", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a machine to delete.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnEditMachine_Click(object sender, RoutedEventArgs e)
        {
            if (dgMachines.SelectedItem is System.Data.DataRowView row)
            {
                int machineId = Convert.ToInt32(row["ID"]);
                string loc = row["Name"].ToString() ?? "";
                string address = row["Address"].ToString() ?? "";
                string status = row["Status"].ToString() ?? "Active";
                double? latitude = row.Row.Table.Columns.Contains("_Latitude") && row["_Latitude"] != DBNull.Value ? Convert.ToDouble(row["_Latitude"]) : null;
                double? longitude = row.Row.Table.Columns.Contains("_Longitude") && row["_Longitude"] != DBNull.Value ? Convert.ToDouble(row["_Longitude"]) : null;

                var editMach = new EditMachineWindow(loc, address, status, latitude, longitude) { Owner = this };
                if (editMach.ShowDialog() == true)
                {
                    string updatedLocationName = editMach.LocationName;
                    string updatedAddress = editMach.Address;
                    string updatedStatus = editMach.Status;
                    double? updatedLatitude = editMach.Latitude;
                    double? updatedLongitude = editMach.Longitude;
                    bool updated = await RunStoreMutationAsync(() =>
                    {
                        var store = new Data.SupabaseStore();
                        return store.UpdateMachine(machineId, updatedLocationName, updatedAddress, updatedStatus, updatedLatitude, updatedLongitude);
                    }, "Update vending machine");

                    if (updated)
                    {
                        await RefreshMachinesAndInventoryAsync();
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

        private async Task LoadUsersDataAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataView view = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.GetUsers().DefaultView;
                });
                dgUsers.ItemsSource = view;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void LoadCatalogItems()
        {
            var store = new Data.SupabaseStore();
            var view = store.GetCatalogItems().DefaultView;
            view.Sort = "[ID] ASC";
            dgItems.ItemsSource = view;
        }

        private async Task LoadCatalogItemsAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataView view = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    var catalogView = store.GetCatalogItems().DefaultView;
                    catalogView.Sort = "[ID] ASC";
                    return catalogView;
                });
                dgItems.ItemsSource = view;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task LoadRecycleCatalogAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataView view = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    var recycleView = store.GetRecyclableCatalog().DefaultView;
                    recycleView.Sort = "[Sort Order] ASC, [Display Name] ASC";
                    return recycleView;
                });
                dgRecycleItems.ItemsSource = view;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private static RecyclableItemDefinition? BuildRecyclableItemFromRow(DataRowView row)
        {
            return new RecyclableItemDefinition
            {
                Id = Convert.ToInt32(row["ID"]),
                DisplayName = row["Display Name"]?.ToString() ?? string.Empty,
                MaterialType = row["Material Type"]?.ToString() ?? string.Empty,
                UnitLabel = row["Unit Label"]?.ToString() ?? "piece",
                PointsPerUnit = Convert.ToInt32(row["Points / Unit"]),
                Description = row["Description"]?.ToString() ?? string.Empty,
                IsActive = string.Equals(row["Active"]?.ToString(), "Active", StringComparison.OrdinalIgnoreCase),
                SortOrder = Convert.ToInt32(row["Sort Order"])
            };
        }

        private async void BtnAddCatalogItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = new CatalogItemWindow { Owner = this };
            if (editor.ShowDialog() == true)
            {
                string itemName = editor.ItemName;
                string itemType = editor.ItemType;
                decimal price = editor.Price;
                int calories = editor.Calories;
                string imagePath = editor.ImagePath;
                string dispenseMessage = editor.DispenseMessage;
                string examineMessage = editor.ExamineMessage;

                bool added = await RunStoreMutationAsync(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.AddCatalogItem(itemName, itemType, price, calories, imagePath, dispenseMessage, examineMessage);
                }, "Create global item");

                if (added)
                {
                    await LoadCatalogItemsAsync();
                }
            }
        }

        private async void BtnEditCatalogItem_Click(object sender, RoutedEventArgs e)
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
                string updatedItemName = editor.ItemName;
                string updatedItemType = editor.ItemType;
                decimal updatedPrice = editor.Price;
                int updatedCalories = editor.Calories;
                string updatedImagePath = editor.ImagePath;
                string updatedDispenseMessage = editor.DispenseMessage;
                string updatedExamineMessage = editor.ExamineMessage;

                bool updated = await RunStoreMutationAsync(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.UpdateCatalogItem(itemId, updatedItemName, updatedItemType, updatedPrice, updatedCalories, updatedImagePath, updatedDispenseMessage, updatedExamineMessage);
                }, "Update global item");

                if (updated)
                {
                    await RefreshCatalogAndSelectedInventoryAsync();
                }
            }
        }

        private async void BtnDeleteCatalogItem_Click(object sender, RoutedEventArgs e)
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

            bool deleted = await RunStoreMutationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.DeleteCatalogItem(itemId);
            }, "Delete global item");

            if (deleted)
            {
                await RefreshCatalogAndSelectedInventoryAsync();
            }
        }

        private async void BtnAddRecycleItem_Click(object sender, RoutedEventArgs e)
        {
            var editor = new RecyclableItemWindow { Owner = this };
            if (editor.ShowDialog() != true)
            {
                return;
            }

            bool added = await RunStoreMutationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.AddRecyclableItem(
                    editor.DisplayNameValue,
                    editor.MaterialType,
                    editor.UnitLabel,
                    editor.PointsPerUnit,
                    editor.DescriptionValue,
                    editor.IsActiveValue,
                    editor.SortOrder);
            }, "Create recyclable item");

            if (added)
            {
                await LoadRecycleCatalogAsync();
            }
        }

        private async void BtnEditRecycleItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgRecycleItems.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Please select a recycle item to edit.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RecyclableItemDefinition? recyclableItem = BuildRecyclableItemFromRow(row);
            if (recyclableItem == null)
            {
                return;
            }

            var editor = new RecyclableItemWindow(recyclableItem) { Owner = this };
            if (editor.ShowDialog() != true)
            {
                return;
            }

            bool updated = await RunStoreMutationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.UpdateRecyclableItem(
                    recyclableItem.Id,
                    editor.DisplayNameValue,
                    editor.MaterialType,
                    editor.UnitLabel,
                    editor.PointsPerUnit,
                    editor.DescriptionValue,
                    editor.IsActiveValue,
                    editor.SortOrder);
            }, "Update recyclable item");

            if (updated)
            {
                await LoadRecycleCatalogAsync();
            }
        }

        private async void BtnToggleRecycleItem_Click(object sender, RoutedEventArgs e)
        {
            if (dgRecycleItems.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Please select a recycle item to activate or deactivate.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RecyclableItemDefinition? recyclableItem = BuildRecyclableItemFromRow(row);
            if (recyclableItem == null)
            {
                return;
            }

            bool targetState = !recyclableItem.IsActive;
            string actionLabel = targetState ? "activate" : "deactivate";
            if (MessageBox.Show(
                    $"Are you sure you want to {actionLabel} '{recyclableItem.DisplayName}'?",
                    "Update Recycle Item Status",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            bool updated = await RunStoreMutationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.SetRecyclableItemActive(recyclableItem.Id, targetState);
            }, "Update recyclable item status");

            if (updated)
            {
                await LoadRecycleCatalogAsync();
            }
        }

        private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var editor = new UserEditorWindow { Owner = this };
            if (editor.ShowDialog() == true)
            {
                string username = editor.Username;
                string password = editor.Password;
                int roleId = editor.RoleId;
                List<int> assignedMachineIds = editor.AssignedMachineIds.ToList();

                bool added = await RunStoreMutationAsync(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.AddUser(username, password, roleId, assignedMachineIds);
                }, "Create staff account");

                if (added)
                {
                    await LoadUsersDataAsync();
                }
                else
                {
                    MessageBox.Show("Could not add user. Username may already exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnEditUserAssignments_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsers.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Please select a staff account.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int userId = Convert.ToInt32(row["ID"]);
            string username = row["Username"].ToString() ?? string.Empty;
            List<int> assignedMachineIds = ParseAssignedMachineIds(row["_AssignedMachineIds"].ToString() ?? string.Empty);

            var editor = new UserEditorWindow(username, assignedMachineIds) { Owner = this };
            if (editor.ShowDialog() != true)
            {
                return;
            }

            bool updated = await RunStoreMutationAsync(() =>
            {
                var store = new Data.SupabaseStore();
                return store.UpdateUserMachineAssignments(userId, editor.AssignedMachineIds);
            }, "Update staff machine assignments");

            if (updated)
            {
                await LoadUsersDataAsync();
            }
            else
            {
                MessageBox.Show("Could not update staff machine assignments.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static List<int> ParseAssignedMachineIds(string value)
        {
            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => int.TryParse(part, out int id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
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
                    bool deleted = await RunStoreMutationAsync(() =>
                    {
                        var store = new Data.SupabaseStore();
                        return store.DeleteUser(userId);
                    }, "Delete staff account");

                    if (deleted)
                    {
                        await LoadUsersDataAsync();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete the user.", "Delete User", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async Task LoadCustomersDataAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                DataView view = await Task.Run(() =>
                {
                    var store = new Data.SupabaseStore();
                    return store.GetCustomers().DefaultView;
                });
                dgCustomers.ItemsSource = view;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void BtnEditCustomerCredits_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is System.Data.DataRowView row)
            {
                string rfid = row["RFID"].ToString() ?? "";
                int currentPoints = Convert.ToInt32(row["Points"]);

                var editor = new PointAmountWindow(
                    "Modify Eco-Credit Balance",
                    $"RFID: {rfid}\nCurrent balance: {currentPoints} points. Enter the exact new balance.",
                    currentPoints,
                    0,
                    999999)
                {
                    Owner = this
                };

                if (editor.ShowDialog() == true)
                {
                    int newPoints = editor.PointAmount;
                    bool updated = await RunStoreMutationAsync(() =>
                    {
                        var store = new Data.SupabaseStore();
                        return store.UpdateCustomerCredits(rfid, newPoints);
                    }, "Update customer credits");

                    if (updated)
                    {
                        await LoadCustomersDataAsync();
                        MessageBox.Show(
                            this,
                            $"Customer balance updated from {currentPoints} to {newPoints} points.",
                            "Modify Credit",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to update customer credits.", "Modify Credit", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a customer.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnDeleteCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is System.Data.DataRowView row)
            {
                string rfid = row["RFID"].ToString() ?? "";
                if (MessageBox.Show($"Are you sure you want to permanently delete customer with RFID '{rfid}'?", "Delete Customer", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    bool deleted = await RunStoreMutationAsync(() =>
                    {
                        var store = new Data.SupabaseStore();
                        return store.DeleteCustomer(rfid);
                    }, "Delete customer");

                    if (deleted)
                    {
                        await LoadCustomersDataAsync();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete the customer.", "Delete Customer", MessageBoxButton.OK, MessageBoxImage.Error);
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
