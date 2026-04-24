using System.Windows;
using System.Windows.Input;
using Eco_Matic.Data;

namespace Eco_Matic
{
    public partial class MainWindow : Window
    {
        private Data.ArduinoService? _arduino;
        private Data.SupabaseStore? _db;
        private CustomerWindow? _activeCustomerWindow;
        private int _openCustomerWindows;
        private bool _isHandlingRfidScan;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            try
            {
                _db = new Data.SupabaseStore();
                _db.EnsureCustomerTableExists(); // Compatibility no-op
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Supabase could not be initialized.\n\n{ex.Message}",
                    "Startup Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            try
            {
                _arduino = Data.ArduinoService.FromEnvironment();
                _arduino.OnCardScanned += Arduino_OnCardScanned;
                _arduino.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Arduino service could not be initialized.\n\n{ex.Message}",
                    "Hardware Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void Arduino_OnCardScanned(object? sender, string rfid)
        {
            if (_isHandlingRfidScan)
            {
                _arduino?.SendMessage("RFID BUSY WAIT");
                return;
            }

            _isHandlingRfidScan = true;

            try
            {
                Data.ArduinoService? arduino = _arduino;
                Data.SupabaseStore? db = _db;
                if (db == null || arduino == null)
                {
                    arduino?.SendMessage("RFID NOT READY");
                    return;
                }

                bool canUseRfid = await Task.Run(() =>
                {
                    return OfflineSyncCoordinator.Instance.CanUseOnlineOnlyFeature(out _);
                });

                if (!canUseRfid)
                {
                    arduino.SendMessage("RFID OFFLINE TRY AGAIN");
                    arduino.SendResponse(false);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        OfflineSyncCoordinator.Instance.CanUseOnlineOnlyFeature(out string offlineMessage);
                        MessageBox.Show(this, offlineMessage, "RFID Requires Internet", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    return;
                }

                arduino.SendMessage("CHECKING DATABASE");
                bool customerExists = await Task.Run(() => db.CustomerExists(rfid));

                if (customerExists)
                {
                    arduino.SendResponse(true);
                    arduino.SendMessage("WELCOME BACK");

                    await Dispatcher.InvokeAsync(() =>
                    {
                        var dashboard = new CustomerDashboardWindow(rfid);
                        dashboard.Owner = this;
                        dashboard.ShowDialog();

                        if (dashboard.SaveSucceeded)
                        {
                            _activeCustomerWindow?.MarkPendingPointsSaved();
                            arduino.SendMessage($"{dashboard.SavedPoints} POINTS SAVED");
                        }
                        else if (DataStore.PendingPoints > 0)
                        {
                            arduino.SendMessage("NO POINTS SAVED");
                        }
                    });
                }
                else
                {
                    arduino.SendResponse(false);
                    arduino.SendMessage("NEW USER REGISTER");

                    await Dispatcher.InvokeAsync(() =>
                    {
                        var registerWindow = new CustomerRegistrationWindow(rfid);
                        registerWindow.Owner = this;
                        if (registerWindow.ShowDialog() == true)
                        {
                            var dashboard = new CustomerDashboardWindow(rfid);
                            dashboard.Owner = this;
                            dashboard.ShowDialog();

                            if (dashboard.SaveSucceeded)
                            {
                                _activeCustomerWindow?.MarkPendingPointsSaved();
                                arduino.SendMessage($"{dashboard.SavedPoints} POINTS SAVED");
                            }
                            else
                            {
                                arduino.SendMessage("CARD REGISTERED");
                            }
                        }
                        else
                        {
                            arduino.SendMessage("REGISTER CANCELLED");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _arduino?.SendResponse(false);
                _arduino?.SendMessage("RFID APP ERROR");
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(this,
                        $"RFID handling failed.\n\n{ex.Message}",
                        "RFID Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                _isHandlingRfidScan = false;
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _arduino?.Stop();
            base.OnClosed(e);
        }

        private void WindowFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnMinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaxRestoreWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            BtnExit_Click(sender, e);
        }

        private async void BtnCustomer_Click(object sender, RoutedEventArgs e)
        {
            btnCustomer.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            (bool canEnter, string entryMessage) = await OfflineSyncCoordinator.Instance.PrepareCustomerModeAsync();
            if (!canEnter)
            {
                Mouse.OverrideCursor = null;
                btnCustomer.IsEnabled = true;
                MessageBox.Show(this, entryMessage, "Customer Mode Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectionWindow = new MachineSelectionWindow
            {
                Owner = this
            };

            Mouse.OverrideCursor = null;
            btnCustomer.IsEnabled = true;

            if (selectionWindow.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var loadedProducts = await System.Threading.Tasks.Task.Run(() =>
                {
                    return DataStore.TryGetProductsForMachine(
                        selectionWindow.SelectedMachineId,
                        out var products)
                        ? products
                        : new List<Product>();
                });
                Mouse.OverrideCursor = null;

                if (loadedProducts.Count == 0)
                {
                    MessageBox.Show(this,
                        DataStore.IsOffline
                            ? "The selected machine does not have any local MySQL demo inventory configured."
                            : "The selected machine does not have any inventory configured in Supabase.",
                        "No Machine Inventory",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var customerWindow = new CustomerWindow(
                    selectionWindow.SelectedMachineId,
                    selectionWindow.SelectedMachineDisplayName,
                    selectionWindow.SelectedMachineAddress,
                    loadedProducts,
                    _arduino)
                {
                    Owner = this
                };

                _openCustomerWindows++;
                _activeCustomerWindow = customerWindow;
                _arduino?.SendMessage("LOADING MACHINE");

                customerWindow.Closed += (_, _) =>
                {
                    OfflineSyncCoordinator.Instance.BeginBackgroundSync();
                    _openCustomerWindows = Math.Max(0, _openCustomerWindows - 1);
                    if (ReferenceEquals(_activeCustomerWindow, customerWindow))
                    {
                        _activeCustomerWindow = null;
                    }

                    if (_openCustomerWindows == 0)
                    {
                        _arduino?.SendCustomerSessionAfk();
                    }
                };
                customerWindow.Show();
            }
        }

        private async void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow
            {
                Owner = this
            };

            if (login.ShowDialog() == true)
            {
                string username = login.Username;
                string password = login.Password;

                btnAdmin.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;

                var loginResult = await System.Threading.Tasks.Task.Run(() =>
                {
                    var store = new Eco_Matic.Data.SupabaseStore();
                    return store.AuthenticateUser(username, password);
                });

                Mouse.OverrideCursor = null;
                btnAdmin.IsEnabled = true;
                string? role = loginResult.Role;
                int? machineId = loginResult.AssignedMachineId;

                if (role != null)
                {
                    Hide();
                    var adminWindow = new AdminWindow(role, machineId)
                    {
                        Owner = this
                    };
                    adminWindow.Closed += (_, _) =>
                    {
                        Show();
                        Activate();
                    };
                    adminWindow.Show();
                }
                else
                {
                    MessageBox.Show(this,
                        "Incorrect authentication.",
                        "Access Denied",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this,
                "Are you sure you want to exit the application?",
                "Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
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
    }
}
