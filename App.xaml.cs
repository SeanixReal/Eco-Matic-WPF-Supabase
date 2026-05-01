using System;
using System.Threading.Tasks;
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
                AppEnvironment.Initialize();
            }
            catch (AppConfigurationException ex)
            {
                MessageBox.Show(
                    $"Eco-Matic could not start because the local configuration is missing or invalid.\n\n{ex.Message}\n\nCreate a .env file in the project root based on .env.example, then restart the app.",
                    "Configuration Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Eco-Matic could not finish startup.\n\n{ex.Message}",
                    "Startup Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            _ = Task.Run(() =>
            {
                try
                {
                    SupabaseSessionCoordinator.Instance.InitializeApplication();
                }
                catch
                {
                    // Customer-mode Supabase availability is checked on demand.
                    // Startup should stay responsive even if Supabase is slow/unavailable.
                }
            });
        }
    }
}
