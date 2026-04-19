using System.Windows;

namespace Eco_Matic
{
    public partial class CustomerRegistrationWindow : Window
    {
        private string _rfid;

        public CustomerRegistrationWindow(string rfid)
        {
            InitializeComponent();
            _rfid = rfid;
            txtRfid.Text = _rfid;
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Please enter both an email and a password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var db = new Data.SupabaseStore();
            if (db.RegisterCustomer(_rfid, email, pass))
            {
                MessageBox.Show("Registration successful! You can now earn Eco-Credits.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Email might already be in use. Please try another.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
