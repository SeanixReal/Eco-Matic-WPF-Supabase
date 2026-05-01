using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Eco_Matic.Data;

namespace Eco_Matic;

public class VendingMachineModel
{
    public int MachineId { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string DisplayName => string.IsNullOrWhiteSpace(Address)
        ? MachineName
        : $"{MachineName} - {Address}";
}

public partial class MachineSelectionWindow : Window
{
    public int SelectedMachineId { get; private set; }
    public string SelectedMachineDisplayName { get; private set; } = string.Empty;
    public string SelectedMachineAddress { get; private set; } = string.Empty;

    public MachineSelectionWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        txtStatus.Text = "Loading machine locations...";
        icMachines.IsEnabled = false;

        try
        {
            var dt = await System.Threading.Tasks.Task.Run(
                () => SupabaseSessionCoordinator.Instance.GetMachineLookupForCustomer());
            var machines = new List<VendingMachineModel>();

            foreach (System.Data.DataRow row in dt.Rows)
            {
                string status = row["status"]?.ToString() ?? "";
                if (status != "Active") continue;

                machines.Add(new VendingMachineModel
                {
                    MachineId = Convert.ToInt32(row["machine_id"]),
                    MachineName = row["location_name"]?.ToString() ?? "Unknown",
                    Address = row.Table.Columns.Contains("address_text") ? row["address_text"]?.ToString() ?? string.Empty : string.Empty
                });
            }

            machines = machines
                .OrderBy(machine => machine.MachineName, StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList();

            if (machines.Count == 0)
            {
                txtStatus.Text = "No active vending machines currently exist in the database.";
                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(214, 90, 90));
            }
            else
            {
                txtStatus.Text = "Please pick a machine location to interact with.";
            }

            icMachines.ItemsSource = machines;
            icMachines.IsEnabled = machines.Count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load machine locations: {ex.Message}", "Machine Source Error", MessageBoxButton.OK, MessageBoxImage.Error);
            txtStatus.Text = "Could not load machine locations.";
        }
    }

    private void BtnMachine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn &&
            btn.DataContext is VendingMachineModel machine &&
            int.TryParse(btn.Tag?.ToString(), out int machineId))
        {
            SelectedMachineId = machineId;
            SelectedMachineDisplayName = machine.MachineName;
            SelectedMachineAddress = machine.Address;
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

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow
        {
            Owner = this
        };
        about.ShowDialog();
    }

    private void OpenReadmeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var readme = new ReadmeWindow
        {
            Owner = this
        };
        readme.ShowDialog();
    }
}
