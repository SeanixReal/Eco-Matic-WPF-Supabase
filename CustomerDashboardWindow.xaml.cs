using System.Windows;

namespace Eco_Matic
{
    public partial class CustomerDashboardWindow : Window
    {
        public CustomerDashboardWindow(string rfid)
        {
            InitializeComponent();
            LoadCustomerData(rfid);
        }

        private void LoadCustomerData(string rfid)
        {
            var db = new Data.MySqlStore();
            var info = db.GetCustomerInfo(rfid);
            
            if (!string.IsNullOrEmpty(info.Email))
            {
                txtWelcome.Text = $"Welcome, {info.Email}";
                txtBalance.Text = info.EcoCredits.ToString();
            }
            else
            {
                txtWelcome.Text = "Welcome, Guest";
                txtBalance.Text = "0";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
