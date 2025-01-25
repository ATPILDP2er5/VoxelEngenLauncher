using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Diagnostics;

namespace VoxelEngenLauncher
{
    public partial class MainWindow : Window
    {
        private List<string?> ListVersion = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (ListVersion.Count == 0) // Чтобы избежать дублирования
            {
                foreach (var item in App.VersionControl)
                {
                    ListVersion.Add(item.Name);
                }
                eCB_ControlVershion.ItemsSource = ListVersion;
            }
        }

        private async void eB_Play_Click(object sender, RoutedEventArgs e)
        {
            if (eCB_ControlVershion.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите версию игры!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedVersion = App.VersionControl[eCB_ControlVershion.SelectedIndex];
            string versionFolder = Path.Combine("GameVersionCore", selectedVersion.Name);
            string versionTag = selectedVersion.Name.Substring(1);
            string fileName = $"voxelcore.{versionTag}_win64.zip";
            selectedVersion.ZipUrl = $"https://github.com/{App.repoOwner}/{App.repoName}/releases/download/{selectedVersion.Name}/{fileName}";

            if (eB_Play.Content.ToString() == "Установить")
            {
                eB_Play.IsEnabled = false;
                await DownloadAndExtractRelease(selectedVersion.ZipUrl, versionFolder, fileName);
                MessageBox.Show($"Версия {selectedVersion.Name} успешно установлена!");
                eB_Play.Content = "Играть";
                eB_Play.IsEnabled = true;
            }
            else if (eB_Play.Content.ToString() == "Играть")
            {
                string exePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    versionFolder,
                    $"voxelcore.{versionTag}_win64",
                    "VoxelCore.exe"
                );

                await LaunchVoxelCoreAsync(exePath);
            }
        }

        private static async Task DownloadAndExtractRelease(string zipUrl, string destinationFolder, string fileName)
        {
            // Формируем название архива

            
            string tempZipFile = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                // Скачиваем файл архива
                using HttpClient client = new HttpClient();
                using (var responseStream = await client.GetStreamAsync(zipUrl))
                using (var fileStream = new FileStream(tempZipFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await responseStream.CopyToAsync(fileStream);
                }

                // Распаковываем архив в целевую папку
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                ZipFile.ExtractToDirectory(tempZipFile, destinationFolder, overwriteFiles: true);

                // Удаляем временный файл
                File.Delete(tempZipFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке или распаковке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task LaunchVoxelCoreAsync(string exePath)
        {
            try
            {
                if (!File.Exists(exePath))
                {
                    MessageBox.Show($"Файл VoxelCore.exe не найден: {exePath}");
                    return;
                }

                using Process process = new()
                {
                    StartInfo =
                    {
                        FileName = exePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(exePath)
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске VoxelCore.exe: {ex.Message}");
            }
        }

        private void eCB_ControlVershion_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (eCB_ControlVershion.SelectedIndex == -1)
                return;

            var selectedVersion = App.VersionControl[eCB_ControlVershion.SelectedIndex];
            string versionFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameVersionCore", selectedVersion.Name);

            eB_Play.Content = Directory.Exists(versionFolder) ? "Играть" : "Установить";
            eB_Play.IsEnabled = true;
        }
    }
}

