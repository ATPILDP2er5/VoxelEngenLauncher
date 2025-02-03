using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;
using VoxelEngenLauncherRepack.Layouts;

namespace VoxelEngenLauncherRepack
{
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            var DEF = new DEFOLT_ZATICHKA();
            var loadingWindow = new Window1();

            loadingWindow.ShowDialog();
            var mainWindow = new MainWindow();
            if (mainWindow.ShowDialog() == true)
            {
            }
            DEF.Close();
        }
    }
}
