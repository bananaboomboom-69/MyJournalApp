using System.Windows;

namespace MyJournalApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the database on startup.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize database service (creates tables if not exist)
            _ = Services.DatabaseService.Instance;
        }
    }
}
