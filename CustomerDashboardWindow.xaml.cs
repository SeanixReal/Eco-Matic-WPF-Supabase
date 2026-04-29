using System.Windows;
using System.Data;
using Eco_Matic.Data;

namespace Eco_Matic
{
    public partial class CustomerDashboardWindow : Window
    {
        private readonly string _rfid;
        private readonly bool _allowPendingPointSave;
        private readonly DataTable? _supplementalHistory;

        public int SavedPoints { get; private set; }
        public int FinalBalance { get; private set; }
        public string CustomerEmail { get; private set; } = string.Empty;
        public bool SaveSucceeded { get; private set; }

        public CustomerDashboardWindow(string rfid, bool allowPendingPointSave = true, DataTable? supplementalHistory = null)
        {
            InitializeComponent();
            _rfid = rfid;
            _allowPendingPointSave = allowPendingPointSave;
            _supplementalHistory = supplementalHistory;
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
            txtWelcome.Text = "Synchronizing account...";

            var result = await Task.Run(() =>
            {
                var db = new Data.SupabaseStore();
                var info = db.GetCustomerInfo(rfid);
                int finalBalance = info.EcoCredits;
                int savedPoints = 0;
                bool saveSucceeded = false;
                bool saveFailed = false;

                if (_allowPendingPointSave && DataStore.PendingPoints > 0)
                {
                    int pointsToSave = DataStore.PendingPoints;
                    finalBalance += pointsToSave;
                    if (db.UpdateCustomerCredits(rfid, finalBalance))
                    {
                        savedPoints = pointsToSave;
                        saveSucceeded = true;
                        DataStore.PendingPoints = Math.Max(0, DataStore.PendingPoints - pointsToSave);
                        db.LogEvent("POINTS_SAVED", $"{pointsToSave} points saved via RFID ({rfid})");
                    }
                    else
                    {
                        finalBalance = info.EcoCredits;
                        saveFailed = true;
                    }
                }

                var history = db.GetCustomerTransactionHistory(rfid);

                return new
                {
                    info.Email,
                    FinalBalance = finalBalance,
                    SavedPoints = savedPoints,
                    SaveSucceeded = saveSucceeded,
                    SaveFailed = saveFailed,
                    History = history
                };
            });

            SavedPoints = result.SavedPoints;
            FinalBalance = result.FinalBalance;
            CustomerEmail = result.Email;
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

            MergeSupplementalHistory(result.History, _supplementalHistory);
            dgAccountHistory.ItemsSource = result.History.DefaultView;

            btnClose.IsEnabled = true;
        }

        private static void MergeSupplementalHistory(DataTable history, DataTable? supplementalHistory)
        {
            if (supplementalHistory == null || supplementalHistory.Rows.Count == 0)
            {
                return;
            }

            foreach (DataRow supplementalRow in supplementalHistory.Rows)
            {
                bool alreadyPresent = history.Rows.Cast<DataRow>().Any(row =>
                    string.Equals(row["Item"]?.ToString(), supplementalRow["Item"]?.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(row["Paid"]?.ToString(), supplementalRow["Paid"]?.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    Convert.ToInt32(row["Quantity"]) == Convert.ToInt32(supplementalRow["Quantity"]));

                if (alreadyPresent)
                {
                    continue;
                }

                history.ImportRow(supplementalRow);
            }

            DataView sorted = history.DefaultView;
            sorted.Sort = "Timestamp DESC";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
