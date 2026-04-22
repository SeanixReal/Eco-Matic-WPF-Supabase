using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Eco_Matic.Data;

namespace Eco_Matic;

public class VendingMachineModel
{
    public int MachineId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public partial class MachineSelectionWindow : Window
{
    public int SelectedMachineId { get; private set; }

    public MachineSelectionWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var dt = OfflineSyncCoordinator.Instance.GetMachineLookupForCustomer();
            var machines = new List<VendingMachineModel>();

            foreach (System.Data.DataRow row in dt.Rows)
            {
                string status = row["status"]?.ToString() ?? "";
                if (status != "Active") continue;

                machines.Add(new VendingMachineModel
                {
                    MachineId = Convert.ToInt32(row["machine_id"]),
                    DisplayName = row["location_name"]?.ToString() ?? "Unknown"
                });
            }

            if (machines.Count == 0)
            {
                txtStatus.Text = "No active vending machines currently exist in the database.";
                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(214, 90, 90));
            }

            icMachines.ItemsSource = machines;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load cached vending machines: {ex.Message}", "Offline Cache Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnMachine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int machineId))
        {
            SelectedMachineId = machineId;
            DialogResult = true;
            Close();
        }
    }

    private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void WindowFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
