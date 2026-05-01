using System.Windows;
using Eco_Matic.Data;
using Eco_Matic.Utilities;

namespace Eco_Matic;

public partial class ReceiptWindow : Window
{
    private readonly Transaction? _transaction;

    public ReceiptWindow(Transaction? transaction, ReceiptPrintResult? printResult = null)
    {
        InitializeComponent();
        _transaction = transaction;
        PopulateReceipt(transaction);
        ApplyPrintResult(printResult);
    }

    private void PopulateReceipt(Transaction? transaction)
    {
        itemsList.Items.Clear();

        if (transaction == null)
        {
            lblReceiptMeta.Text = "No session data";
            lblMachineName.Text = "Machine: -";
            lblMachineAddress.Text = "Address: -";
            lblTotal.Text = "Total:  PHP 0.00";
            lblPaid.Text = "Cash/QR Paid: PHP 0.00";
            lblPointsUsed.Text = "Points Used: 0";
            lblPointsEarned.Text = "Points Earned: 0";
            lblPointBalance.Text = "Point Balance: -";
            lblChange.Text = "Change: PHP 0.00";
            return;
        }

        lblReceiptMeta.Text = $"{transaction.ReceiptNumber}  |  {transaction.SessionEndedAt:yyyy-MM-dd HH:mm:ss}";
        string machineName = string.IsNullOrWhiteSpace(transaction.MachineDisplayName)
            ? $"Machine {transaction.MachineId}"
            : transaction.MachineDisplayName;
        lblMachineName.Text = $"Machine: {machineName}";
        lblMachineAddress.Text = string.IsNullOrWhiteSpace(transaction.MachineAddress)
            ? "Address: -"
            : $"Address: {transaction.MachineAddress}";

        foreach (var item in transaction.Items)
        {
            string line = $"{item.Quantity}x  {item.ProductName,-16} PHP {item.LineTotal:F2}";
            itemsList.Items.Add(line);

            if (item.WasPaidWithPoints)
            {
                itemsList.Items.Add($"    paid with {item.PointsSpent} eco points");
            }
            else if (item.CashPaid > 0m)
            {
                itemsList.Items.Add($"    paid with PHP {item.CashPaid:F2}");
            }
        }

        foreach (var recycle in transaction.RecycledItems)
        {
            string unitLabel = string.IsNullOrWhiteSpace(recycle.UnitLabel) ? "item" : recycle.UnitLabel;
            string line = $"Recycle {recycle.DisplayName,-16} {recycle.Pieces,3} {unitLabel}(s) +{recycle.TotalPoints} Pts";
            itemsList.Items.Add(line);
        }

        if (itemsList.Items.Count == 0)
        {
            itemsList.Items.Add("No purchased or recycled items in this session.");
        }

        lblTotal.Text = $"Total:  PHP {transaction.TotalAmount:F2}";
        lblPaid.Text = $"Cash/QR Paid: PHP {transaction.AmountPaid:F2}";
        lblPointsUsed.Text = transaction.EcoPointsSpent > 0
            ? $"Points Used: {transaction.EcoPointsSpent} ({transaction.SessionPointsSpent} session, {transaction.SavedEcoCreditsSpent} saved)"
            : "Points Used: 0";
        lblPointsEarned.Text = $"Points Earned: {transaction.RecyclePointsTotal}";
        lblPointBalance.Text = transaction.EcoCreditBalanceAfter.HasValue
            ? $"RFID Balance After: {transaction.EcoCreditBalanceAfter.Value} pts"
            : $"Points To Save: {transaction.UnsavedSessionPointsRemaining} pts";
        lblChange.Text = $"Change: PHP {transaction.Change:F2}";
    }

    private void ApplyPrintResult(ReceiptPrintResult? printResult)
    {
        if (printResult == null)
        {
            btnPrint.Content = "Print";
            return;
        }

        btnPrint.Content = printResult.Success ? "Reprint" : "Print Again";
    }

    private async void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        if (_transaction == null)
        {
            return;
        }

        btnPrint.IsEnabled = false;
        AudioService.StopAllAudio();
        await Task.Delay(1500); // Allow Bluetooth bandwidth to clear
        var printResult = await Task.Run(() => ReceiptPrinterService.Instance.TryPrintReceipt(_transaction));
        btnPrint.IsEnabled = true;
        ApplyPrintResult(printResult);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
