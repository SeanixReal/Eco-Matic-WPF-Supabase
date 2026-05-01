using System.Windows;
using Eco_Matic.Data;
using MessageBox = Eco_Matic.Utilities.WindowDialog;

namespace Eco_Matic
{
    public partial class CustomerRegistrationWindow : Window
    {
        private readonly string _rfid;
        private bool _isRegistering;

        public CustomerRegistrationWindow(string rfid)
        {
            InitializeComponent();
            _rfid = rfid;
            txtRfid.Text = _rfid;
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (!SupabaseSessionCoordinator.Instance.CanUseSupabaseFeature(out string connectivityMessage))
            {
                MessageBox.Show(this, connectivityMessage, "Registration Requires Internet", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string email = txtEmail.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Please enter both an email and a password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isRegistering)
            {
                return;
            }

            SetRegistrationBusy(true);
            try
            {
                bool registered = await RunWithTimeoutAsync(() =>
                {
                    var db = new Data.SupabaseStore();
                    return db.RegisterCustomer(_rfid, email, pass);
                }, TimeSpan.FromSeconds(10));

                if (registered)
                {
                    MessageBox.Show("Registration successful! You can now earn Eco-Credits.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Registration failed. The email may already be in use, or Supabase may be unavailable.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show(this,
                    "Registration is taking too long. Please check your internet connection and try again.",
                    "Registration Timeout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                SetRegistrationBusy(false);
            }
        }

        private void SetRegistrationBusy(bool isBusy)
        {
            _isRegistering = isBusy;
            btnRegister.IsEnabled = !isBusy;
            btnCancel.IsEnabled = !isBusy;
            txtEmail.IsEnabled = !isBusy;
            txtPassword.IsEnabled = !isBusy;
            btnRegister.Content = isBusy ? "Registering..." : "Register";
        }

        private static async Task<T> RunWithTimeoutAsync<T>(Func<T> work, TimeSpan timeout)
        {
            Task<T> workTask = Task.Run(work);
            Task delayTask = Task.Delay(timeout);

            Task completedTask = await Task.WhenAny(workTask, delayTask);
            if (completedTask == delayTask)
            {
                throw new TimeoutException();
            }

            return await workTask;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
