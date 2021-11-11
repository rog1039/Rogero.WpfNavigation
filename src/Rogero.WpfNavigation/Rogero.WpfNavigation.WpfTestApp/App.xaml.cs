using System.Windows;

namespace Rogero.WpfNavigation.WpfTestApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        var routerVm = new RoutingTestWindowViewModel();
        var window   = new RoutingTestWindow() { DataContext = routerVm };
        window.Show();
    }
}