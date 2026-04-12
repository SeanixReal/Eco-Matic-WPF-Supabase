using System.Windows;
using System.Windows.Input;

namespace Eco_Matic
{
    public partial class MainWindow : Window
    {
        private const string AdminPassword = "admin123";

        public MainWindow()
        {
            InitializeComponent();
            UpdateExitButton();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            UpdateExitButton();
        }

        private void UpdateExitButton()
        {
            if (DataStore.LastTransaction != null)
            {
                btnExit.Content = "Print Receipt & Finish";
                btnExit.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 119, 230));
            }
            else
            {
                btnExit.Content = "Exit Station";
                btnExit.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(214, 90, 90));
            }
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
                var customerWindow = new CustomerWindow
                {
                    Owner = this
                };
                
                customerWindow.Closed += (_, _) =>
                {
                    Show();
                    Activate();
                    UpdateExitButton();
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
                var store = new Eco_Matic.Data.MySqlStore();
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
                        UpdateExitButton();
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
            if (DataStore.LastTransaction != null)
            {
                var receipt = new ReceiptWindow(DataStore.LastTransaction)
                {
                    Owner = this
                };
                receipt.ShowDialog();
                DataStore.LastTransaction = null;
                UpdateExitButton();
                return;
            }

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