using System.Windows;
using System.Windows.Input;
using System.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Eco_Matic.Data;
using Eco_Matic.Utilities;

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

            _ = RefreshConnectivityBadgeAsync();
        }

        private async Task RefreshConnectivityBadgeAsync()
        {
            SetConnectivityBadge("Checking Supabase...", "#FFF7E6", "#F2C66D", "#C98000", "#7A4B00", 1.0);

            bool connected = await Task.Run(() =>
                SupabaseSessionCoordinator.Instance.RefreshAvailabilityStatus());

            if (connected)
            {
                SetConnectivityBadge("Supabase connected", "#ECFDF3", "#8AD6A8", "#1B9C67", "#0F6B43", 1.0);

                var hideTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(4)
                };
                hideTimer.Tick += (_, _) =>
                {
                    hideTimer.Stop();
                    pnlConnectivityStatus.Visibility = Visibility.Collapsed;
                };
                hideTimer.Start();
                return;
            }

            SetConnectivityBadge("Supabase offline - data features need internet", "#FFF1F1", "#F1B7B7", "#D65A5A", "#9B2C2C", 1.0);
        }

        private void SetConnectivityBadge(
            string text,
            string background,
            string border,
            string dot,
            string foreground,
            double opacity)
        {
            pnlConnectivityStatus.Visibility = Visibility.Visible;
            pnlConnectivityStatus.Opacity = opacity;
            pnlConnectivityStatus.Background = (Brush)new BrushConverter().ConvertFromString(background)!;
            pnlConnectivityStatus.BorderBrush = (Brush)new BrushConverter().ConvertFromString(border)!;
            dotConnectivityStatus.Fill = (Brush)new BrushConverter().ConvertFromString(dot)!;
            txtConnectivityStatus.Foreground = (Brush)new BrushConverter().ConvertFromString(foreground)!;
            txtConnectivityStatus.Text = text;
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
                    return SupabaseSessionCoordinator.Instance.CanUseSupabaseFeature(out _);
                });

                if (!canUseRfid)
                {
                    arduino.SendMessage("RFID OFFLINE TRY AGAIN");
                    arduino.SendResponse(false);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        SupabaseSessionCoordinator.Instance.CanUseSupabaseFeature(out string connectivityMessage);
                        MessageBox.Show(this, connectivityMessage, "RFID Requires Internet", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    return;
                }

                arduino.SendMessage("CHECKING DATABASE");
                bool customerExists = await Task.Run(() => db.CustomerExists(rfid));

                if (customerExists)
                {
                    arduino.SendResponse(true);
                    arduino.SendMessage("WELCOME BACK");
                    Eco_Matic.Utilities.AudioService.SpeakAsync("Welcome back!");

                    await Dispatcher.InvokeAsync(() =>
                    {
                        bool allowSessionRfid = _activeCustomerWindow?.CanUseRfidForCurrentSession(rfid) ?? true;
                        DataTable? sessionHistory = null;
                        if (allowSessionRfid)
                        {
                            _activeCustomerWindow?.PrepareRfidForCurrentSession(rfid);
                            sessionHistory = _activeCustomerWindow?.GetCurrentSessionTransactionHistory(rfid);
                        }
                        else
                        {
                            arduino.SendMessage("SESSION RFID LOCKED");
                        }

                        var dashboard = new CustomerDashboardWindow(rfid, allowSessionRfid, sessionHistory);
                        dashboard.Owner = this;
                        dashboard.ShowDialog();

                        if (dashboard.SaveSucceeded)
                        {
                            _activeCustomerWindow?.MarkPendingPointsSaved(dashboard.SavedPoints);
                            arduino.SendMessage($"{dashboard.SavedPoints} POINTS SAVED");
                            Eco_Matic.Utilities.AudioService.PlaySfx("Assets/Audio/success.mp3");
                        }
                        else if (DataStore.PendingPoints > 0)
                        {
                            arduino.SendMessage("NO POINTS SAVED");
                        }

                        if (allowSessionRfid)
                        {
                            _activeCustomerWindow?.SetLinkedRfidCustomer(rfid, dashboard.CustomerEmail, dashboard.FinalBalance);
                        }
                        else
                        {
                            MessageBox.Show(this,
                                "This vending session already has purchases attached to another RFID card, so this card was shown only for account viewing. Finish the current session before switching cards.",
                                "RFID Session Locked",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    });
                }
                else
                {
                    arduino.SendResponse(false);
                    arduino.SendMessage("NEW USER REGISTER");
                    Eco_Matic.Utilities.AudioService.SpeakAsync("Welcome. Please register your new card.");

                    await Dispatcher.InvokeAsync(() =>
                    {
                        var registerWindow = new CustomerRegistrationWindow(rfid);
                        registerWindow.Owner = this;
                        if (registerWindow.ShowDialog() == true)
                        {
                            bool allowSessionRfid = _activeCustomerWindow?.CanUseRfidForCurrentSession(rfid) ?? true;
                            DataTable? sessionHistory = null;
                            if (allowSessionRfid)
                            {
                                _activeCustomerWindow?.PrepareRfidForCurrentSession(rfid);
                                sessionHistory = _activeCustomerWindow?.GetCurrentSessionTransactionHistory(rfid);
                            }
                            else
                            {
                                arduino.SendMessage("SESSION RFID LOCKED");
                            }

                            var dashboard = new CustomerDashboardWindow(rfid, allowSessionRfid, sessionHistory);
                            dashboard.Owner = this;
                            dashboard.ShowDialog();

                            if (dashboard.SaveSucceeded)
                            {
                                _activeCustomerWindow?.MarkPendingPointsSaved(dashboard.SavedPoints);
                                arduino.SendMessage($"{dashboard.SavedPoints} POINTS SAVED");
                                Eco_Matic.Utilities.AudioService.PlaySfx("Assets/Audio/success.mp3");
                            }
                            else
                            {
                                arduino.SendMessage("CARD REGISTERED");
                                Eco_Matic.Utilities.AudioService.PlaySfx("Assets/Audio/success.mp3");
                            }

                            if (allowSessionRfid)
                            {
                                _activeCustomerWindow?.SetLinkedRfidCustomer(rfid, dashboard.CustomerEmail, dashboard.FinalBalance);
                            }
                            else
                            {
                                MessageBox.Show(this,
                                    "This vending session already has purchases attached to another RFID card, so this newly registered card was not attached to the current purchase history.",
                                    "RFID Session Locked",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
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

            (bool canEnter, string entryMessage) = await SupabaseSessionCoordinator.Instance.PrepareCustomerModeAsync();
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
                        "The selected machine does not have any inventory configured in Supabase.",
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
                    var adminAuth = store.AuthenticateUserAccess(username, password);
                    
                    if (adminAuth.Role != null)
                    {
                        return new { Type = "Admin", Role = (string?)adminAuth.Role, MachineIds = adminAuth.AssignedMachineIds, Rfid = (string?)null };
                    }

                    string? customerRfid = store.AuthenticateCustomer(username, password);
                    if (customerRfid != null)
                    {
                        return new { Type = "Customer", Role = (string?)null, MachineIds = new List<int>(), Rfid = (string?)customerRfid };
                    }

                    return new { Type = "None", Role = (string?)null, MachineIds = new List<int>(), Rfid = (string?)null };
                });

                Mouse.OverrideCursor = null;
                btnAdmin.IsEnabled = true;

                if (loginResult.Type == "Admin")
                {
                    Hide();
                    var adminWindow = new AdminWindow(loginResult.Role!, loginResult.MachineIds)
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
                else if (loginResult.Type == "Customer")
                {
                    string rfid = loginResult.Rfid!;
                    bool allowSessionRfid = _activeCustomerWindow?.CanUseRfidForCurrentSession(rfid) ?? true;
                    DataTable? sessionHistory = null;
                    if (allowSessionRfid)
                    {
                        _activeCustomerWindow?.PrepareRfidForCurrentSession(rfid);
                        sessionHistory = _activeCustomerWindow?.GetCurrentSessionTransactionHistory(rfid);
                    }

                    var dashboard = new CustomerDashboardWindow(rfid, allowSessionRfid, sessionHistory);
                    dashboard.Owner = this;
                    dashboard.ShowDialog();
                    
                    if (dashboard.SaveSucceeded)
                    {
                        _activeCustomerWindow?.MarkPendingPointsSaved(dashboard.SavedPoints);
                    }

                    if (allowSessionRfid)
                    {
                        _activeCustomerWindow?.SetLinkedRfidCustomer(rfid, dashboard.CustomerEmail, dashboard.FinalBalance);
                    }
                    else
                    {
                        MessageBox.Show(this,
                            "This vending session already has purchases attached to another RFID account, so this login was shown only for account viewing.",
                            "Customer Session Locked",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show(this,
                        "Incorrect credentials. If you are a customer, use your registered email and password.",
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
