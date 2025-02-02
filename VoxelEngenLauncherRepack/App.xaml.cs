using System.Configuration;
using System.Data;
using System.Windows;
using VoxelEngenLauncherRepack.Layouts;

namespace VoxelEngenLauncherRepack
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {

            Thread windowThread = new Thread(() =>
            {
                Window1 window = new Window1();
                window.Closed += (s, e) => Application.Current.Dispatcher.InvokeShutdown();
                window.Show();
                System.Windows.Threading.Dispatcher.Run();
            });

            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();
            windowThread.Join();

        }
    }

}
