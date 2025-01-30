using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static VoxelEngenLauncher.App;

namespace VoxelEngenLauncher.Layouts.WindowTab
{
    /// <summary>
    /// Логика взаимодействия для AddCustumVersion.xaml
    /// </summary>
    public partial class AddCustumVersion : Window
    {
        public List<GitHubRelease> ListCors = [];
        public AddCustumVersion()
        {
            InitializeComponent();
            LoadReleases();
        }

        private async void LoadReleases()
        {
            LoadingTab loadingTab = new LoadingTab();
            ListCors = await loadingTab.GetReleasesWithProgressAsync($"https://api.github.com/repos/{repoOwner}/{repoName}/releases");
        }

        private async void eB_CreatePork_Click(object sender, RoutedEventArgs e)
        {
            eB_CreatePork.IsEnabled = false;
            eB_Cancel.IsEnabled = false;
            var selectedVersion = ListCors[eCB_CoreVersionList.SelectedIndex];
            string versionFolder = System.IO.Path.Combine(MainWindow.gameVersionCorePath, eEB_DirectoryName.Text);
            string fileName = $"voxelcore.{selectedVersion.Name.Substring(1)}_win64.zip";
            string ZipUrl = $"https://github.com/{App.repoOwner}/{App.repoName}/releases/download/{selectedVersion.Name}/{fileName}";
            await MainWindow.DownloadAndExtractRelease(ZipUrl, versionFolder, fileName, nePB_Compilate, null);
            DialogResult = true;
        }

        private void eB_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void eB_ModsCatalog_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
