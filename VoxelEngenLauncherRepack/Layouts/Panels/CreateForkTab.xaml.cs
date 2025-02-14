using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VoxelEngenLauncherRepack.Resource.Scripts;

namespace VoxelEngenLauncherRepack.Layouts
{
    
    /// <summary>
    /// Логика взаимодействия для CreateForkTab.xaml
    /// </summary>
    public partial class CreateForkTab : UserControl
    {
        public static List<string> VersionCorses = new List<string>();
        public CreateForkTab()
        {
            InitializeComponent();

            foreach (var iten in App.Releases)
            {
                VersionCorses.Add(iten.Name);
            }
            eCB_CoreVersionList.ItemsSource = VersionCorses;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void eB_CreatePork_Click(object sender, RoutedEventArgs e)
        {
            await NetworkManagerGH.DowloadRelease(App.Releases[eCB_CoreVersionList.SelectedIndex], nePB_Compilate);
            await StorageManager.ExtractCoreFromZIPAsync(App.Releases[eCB_CoreVersionList.SelectedIndex].Name, eEB_DirectoryName.Text ?? "ORIG");

            // Новый путь к форкам в AppData
            string forkDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VEL",
                "Resource",
                "Data",
                "Forks",
                App.Releases[eCB_CoreVersionList.SelectedIndex].Name,
                eEB_DirectoryName.Text ?? "ORIG"
            );

            Directory.CreateDirectory(forkDirectory); // Создать папку, если её нет
        }
    }
}
