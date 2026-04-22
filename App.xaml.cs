using System;
using System.Windows;
using Eco_Matic.Data;

namespace Eco_Matic
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                OfflineSyncCoordinator.Instance.InitializeApplication();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Eco-Matic could not initialize the local offline database.\n\n{ex.Message}\n\nMake sure MySQL is running locally before starting the app.",
                    "Offline Bootstrap Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
