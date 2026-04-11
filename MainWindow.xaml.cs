/*
 * Module: Presentation Layer (MainWindow)
 * Description: Intuitive GUI for customer and admin interactions.
 *              Enforces role-based security via authenticated administrator dashboard.
 */
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
                btnExit.Content = "Get Receipt and Exit";
                btnExit.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 119, 230));
            }
            else
            {
                btnExit.Content = "Exit";
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
            Hide();
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

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow
            {
                Owner = this
            };

            if (login.ShowDialog() == true)
            {
                if (login.Password == AdminPassword)
                {
                    Hide();
                    var adminWindow = new AdminWindow
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
                        "Incorrect password.",
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
                Application.Current.Shutdown();
                return;
            }

            var result = MessageBox.Show(this,
                "Are you sure you want to exit?",
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