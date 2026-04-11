/*
 * Project Title: Eco-Matic Vending Machine Simulator (GUI Edition)
 * Date: 03/20/2026
 * Version: 1.0
 * Description: Promotes SDG 12 (Responsible Consumption) by integrating a standard
 *              vending machine with a unique "trash-to-credit" recycling system.
 *              WPF architecture chosen over WinForms for modern data binding.
 */
using System.Windows;

namespace Eco_Matic
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DataStore.Initialize();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
