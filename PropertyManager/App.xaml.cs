using System.Windows;
using PropertyManager.Data;

namespace PropertyManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize SQLite database (creates file if not exists)
            AppDbContext.Initialize();
        }
    }
}
