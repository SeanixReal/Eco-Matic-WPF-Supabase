using System.Windows;
using Eco_Matic.Data;

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
            if (!OfflineSyncCoordinator.Instance.CanUseOnlineOnlyFeature(out string offlineMessage))
            {
                MessageBox.Show(this, offlineMessage, "RFID Requires Internet", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            var db = new Data.SupabaseStore();
            var info = db.GetCustomerInfo(rfid);

            int finalBalance = info.EcoCredits;
            if (DataStore.PendingPoints > 0)
            {
                finalBalance += DataStore.PendingPoints;
                db.UpdateCustomerCredits(rfid, finalBalance);
                MessageBox.Show(this, $"You successfully saved {DataStore.PendingPoints} recycled points to your RFID account!", "Points Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                DataStore.LogEvent("POINTS_SAVED", $"{DataStore.PendingPoints} points saved via RFID ({rfid})");
                
                // Clear so they don't get saved twice
                DataStore.PendingPoints = 0;
            }
            
            if (!string.IsNullOrEmpty(info.Email))
            {
                txtWelcome.Text = $"Welcome, {info.Email}";
                txtBalance.Text = finalBalance.ToString();
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
