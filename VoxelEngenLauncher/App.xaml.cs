using System.Configuration;
using System.Data;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using VoxelEngenLauncher.Layouts.WindowTab;

namespace VoxelEngenLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string gameDirectory = "GameVersionCore";
        public static string repoOwner = "MihailRis";
        public static string repoName = "VoxelEngine-Cpp";
        public static List<GitHubRelease> VersionControl = [];
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LoadingTab StartProgramm = new LoadingTab();
            MainWindow mainWindow = new MainWindow();
            if (StartProgramm.ShowDialog() == true)
            {
                if (mainWindow.ShowDialog() == true) 
                {
                
                }

            }
            else
            {
                StartProgramm.Close();
            }
        }
        public class GitHubRelease
        {
            public string? Name { get; set; }
            public string? TagName { get; set; }
            public DateTime? PublishedAt { get; set; }
            public string? PathGame { get; set; }
        }
    }

}
