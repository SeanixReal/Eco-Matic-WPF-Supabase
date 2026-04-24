using System.Windows;
using Eco_Matic.Data;

namespace Eco_Matic
{
    public partial class CustomerDashboardWindow : Window
    {
        private readonly string _rfid;

        public int SavedPoints { get; private set; }
        public bool SaveSucceeded { get; private set; }

        public CustomerDashboardWindow(string rfid)
        {
            InitializeComponent();
            _rfid = rfid;
            Loaded += CustomerDashboardWindow_Loaded;
        }

        private async void CustomerDashboardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CustomerDashboardWindow_Loaded;
            await LoadCustomerDataAsync(_rfid);
        }

        private async Task LoadCustomerDataAsync(string rfid)
        {
            if (!OfflineSyncCoordinator.Instance.CanUseOnlineOnlyFeature(out string offlineMessage))
            {
                MessageBox.Show(this, offlineMessage, "RFID Requires Internet", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            btnClose.IsEnabled = false;
            txtWelcome.Text = "Loading RFID account...";

            var result = await Task.Run(() =>
            {
                var db = new Data.SupabaseStore();
                var info = db.GetCustomerInfo(rfid);
                int finalBalance = info.EcoCredits;
                int savedPoints = 0;
                bool saveSucceeded = false;
                bool saveFailed = false;

                if (DataStore.PendingPoints > 0)
                {
                    int pointsToSave = DataStore.PendingPoints;
                    finalBalance += pointsToSave;
                    if (db.UpdateCustomerCredits(rfid, finalBalance))
                    {
                        savedPoints = pointsToSave;
                        saveSucceeded = true;
                        DataStore.LogEvent("POINTS_SAVED", $"{pointsToSave} points saved via RFID ({rfid})");
                        DataStore.PendingPoints = 0;
                    }
                    else
                    {
                        finalBalance = info.EcoCredits;
                        saveFailed = true;
                    }
                }

                return new
                {
                    info.Email,
                    FinalBalance = finalBalance,
                    SavedPoints = savedPoints,
                    SaveSucceeded = saveSucceeded,
                    SaveFailed = saveFailed
                };
            });

            SavedPoints = result.SavedPoints;
            SaveSucceeded = result.SaveSucceeded;

            if (result.SaveSucceeded)
            {
                MessageBox.Show(this, $"You successfully saved {result.SavedPoints} recycled points to your RFID account!", "Points Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (result.SaveFailed)
            {
                MessageBox.Show(this, "Failed to save recycle points to this RFID account. Please check the internet connection and try again.", "Points Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            if (!string.IsNullOrEmpty(result.Email))
            {
                txtWelcome.Text = $"Welcome, {result.Email}";
                txtBalance.Text = result.FinalBalance.ToString();
            }
            else
            {
                txtWelcome.Text = "Welcome, Guest";
                txtBalance.Text = "0";
            }

            btnClose.IsEnabled = true;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
