using System.Windows;
using System.Windows.Input;

namespace Eco_Matic
{
    public partial class MainWindow : Window
    {
        private const string AdminPassword = "admin123";
        private Data.ArduinoService _arduino;
        private Data.SupabaseStore _db;

        public MainWindow()
        {
            InitializeComponent();
            _db = new Data.SupabaseStore();
            _db.EnsureCustomerTableExists(); // Ensure DB is updated on boot
            
            // Connect to Arduino on COM5
            _arduino = new Data.ArduinoService("COM5", 9600);
            _arduino.OnCardScanned += Arduino_OnCardScanned;
            _arduino.Start();
        }

        private void Arduino_OnCardScanned(object? sender, string rfid)
        {
            // The SerialPort event fires on a background thread.
            // We must use Dispatcher.Invoke to do anything visual in WPF.
            Dispatcher.Invoke(() =>
            {
                if (_db.CustomerExists(rfid))
                {
                    _arduino.SendResponse(true); // Turns Green LED on, says "Access Granted"
                    
                    var dashboard = new CustomerDashboardWindow(rfid);
                    dashboard.Owner = this;
                    dashboard.ShowDialog();
                }
                else
                {
                    _arduino.SendResponse(false); // Turns Red LED on, says "Unknown Card"
                    
                    var registerWindow = new CustomerRegistrationWindow(rfid);
                    registerWindow.Owner = this;
                    if (registerWindow.ShowDialog() == true)
                    {
                        // Registration successful, open their dashboard right away
                        var dashboard = new CustomerDashboardWindow(rfid);
                        dashboard.Owner = this;
                        dashboard.ShowDialog();
                    }
                }
            });
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

        private void BtnCustomer_Click(object sender, RoutedEventArgs e)
        {
            var selectionWindow = new MachineSelectionWindow
            {
                Owner = this
            };

            if (selectionWindow.ShowDialog() == true)
            {
                Hide();
                DataStore.Initialize(selectionWindow.SelectedMachineId);
                var customerWindow = new CustomerWindow(_arduino)
                {
                    Owner = this
                };
                
                _arduino.SendStateCommand("STATE:ACTIVE");

                customerWindow.Closed += (_, _) =>
                {
                    _arduino.SendStateCommand("STATE:AFK");
                    Show();
                    Activate();
                };
                customerWindow.Show();
            }
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow
            {
                Owner = this
            };

            if (login.ShowDialog() == true)
            {
                var store = new Eco_Matic.Data.SupabaseStore();
                var loginResult = store.AuthenticateUser(login.Username, login.Password);
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
            MessageBox.Show(this,
                "Eco-Matic Vending Machine\nVersion 1.0\n\nCopyright 2026 Seanix",
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
    }
}