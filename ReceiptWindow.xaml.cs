using System.Windows;

namespace Eco_Matic;

public partial class ReceiptWindow : Window
{
    public ReceiptWindow(Transaction? transaction)
    {
        InitializeComponent();
        PopulateReceipt(transaction);
    }

    private void PopulateReceipt(Transaction? transaction)
    {
        itemsList.Items.Clear();

        if (transaction == null)
        {
            lblTotal.Text = "Total:  PHP 0.00";
            lblPaid.Text = "Paid:   PHP 0.00";
            lblChange.Text = "Change: PHP 0.00";
            return;
        }

        foreach (var item in transaction.Items)
        {
            string line = $"{item.Quantity}x  {item.ProductName,-20} PHP {item.LineTotal:F2}";
            itemsList.Items.Add(line);
        }

        foreach (var recycle in transaction.RecycledItems)
        {
            string line = $"Recycle {recycle.Material,-8} {recycle.WeightKg,5:F2}kg +PHP {recycle.TotalCredit:F2}";
            itemsList.Items.Add(line);
        }

        lblTotal.Text = $"Total:  PHP {transaction.TotalAmount:F2}";
        lblPaid.Text = $"Paid:   PHP {transaction.AmountPaid:F2}";
        lblChange.Text = $"Change: PHP {transaction.Change:F2}";
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
