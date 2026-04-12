using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;

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
        // Load machines from DB
        var store = new Eco_Matic.Data.MySqlStore();
        try
        {
            var machines = new List<VendingMachineModel>();
            using var conn = store.GetConnection();
            conn.Open();

            string query = "SELECT machine_id, location_name FROM vending_machines WHERE status = 'Active'";
            using var cmd = new MySqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            
            while (reader.Read())
            {
                int mId = reader.GetInt32(0);
                string mLoc = reader.GetString(1);
                machines.Add(new VendingMachineModel
                {
                    MachineId = mId,
                    DisplayName = $"Machine {mId} - {mLoc}"
                });
            }

            if (machines.Count == 0)
            {
                txtStatus.Text = "No active vending machines currently exist in the database.";
                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(214, 90, 90));
            }

            icMachines.ItemsSource = machines;
        }
        catch (MySqlException mex) when (mex.Number == 1049) 
        {
            txtStatus.Text = "Database not found. Please run the docs/database_setup.sql script first.";
            txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(214, 90, 90));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load vending machines: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
