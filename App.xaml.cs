using System.Windows;

namespace TibiaSmartScreen;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }
}
