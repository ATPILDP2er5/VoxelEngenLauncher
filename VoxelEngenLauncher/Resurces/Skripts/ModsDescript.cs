using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VoxelEngenLauncher.Layouts.WindowTab;
using static VoxelEngenLauncher.App;

namespace VoxelEngenLauncher.Resurces.Skripts
{
    public class ModsDescript
    {
        public class ModViewPanel
        {
            object Description { get; set; }
            string IconMod { get; set; }
            string NameMod { get; set; }
            string Creator { get; set; }
        }

        public class ModDescription
        {
            public string id { get; set; }
            public string description { get; set; }
            public string title { get; set; }
            public string creator { get; set; }
            public string version { get; set; }
        }
        public class CoreRelises
        {
            public string version { get; set; }
            public string path { get; set; }
        }
        public async Task<List<string>> GetCores()
        {
            List<string> result = new List<string>();
            string apiUrl = $"https://api.github.com/repos/{App.repoOwner}/{App.repoName}/releases";
            LoadingTab loadingTab = new LoadingTab();
            List<App.GitHubRelease> releases = await loadingTab.GetReleasesWithProgressAsync(apiUrl);
            if (releases != null && releases.Count > 0)
            {
                foreach (var release in releases)
                {
                    string versionTag = release.Name.TrimStart('v');
                    string fileName = $"voxelcore.{versionTag}_win64.zip";
                    string fileUrl = $"https://github.com/{repoOwner}/{repoName}/releases/download/{release.Name}/{fileName}";

                    // Проверяем существование файла
                    if (await LoadingTab.CheckFileExistsAsync(fileUrl))
                    {
                        result.Add(versionTag);
                    }
                }
            }
            return result;
        }
        public async void CreateFork(string version, string NameDir, List<string> ModPaths, ProgressBar y)
        {
            string versionFolder = System.IO.Path.Combine(MainWindow.gameVersionCorePath, "v" + version, NameDir);
            string fileName = $"voxelcore.{version}_win64.zip";
            string fileUrl = $"https://github.com/{repoOwner}/{repoName}/releases/download/{version}/{fileName}";
            if (!File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data", "Temp", "coreArchive", fileName)))
            {
                await MainWindow.DownloadAndExtractRelease(fileUrl, versionFolder, fileName, y, null);
            }
            else
            {
                try
                {
                    ZipFile.ExtractToDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data", "Temp", "coreArchive", fileName), versionFolder, true);
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"Ошибка распаковки: {ioEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }


        }


    }
}
