using System.Text;
using System.Windows;

namespace Eco_Matic;

public partial class EventLogWindow : Window
{
    public EventLogWindow()
    {
        InitializeComponent();
        LoadLogs();
    }

    private void LoadLogs()
    {
        var logs = DataStore.ReadLogs();
        if (logs.Count == 0)
        {
            txtLog.Text = "No logs found.";
            return;
        }

        var builder = new StringBuilder();
        foreach (var log in logs)
        {
            builder.AppendLine($"[{log.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC] {log.EventType} | {log.Details} | PHP {log.Amount:F2}");
        }

        txtLog.Text = builder.ToString();
        txtLog.SelectionStart = 0;
        txtLog.SelectionLength = 0;
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadLogs();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
