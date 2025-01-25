using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;

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
                await DownloadAndExtractRelease(selectedVersion.ZipUrl, versionFolder, fileName, nePB_DownloadElement);
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

                await LaunchVoxelCoreAsync(exePath);            }
        }

        private static async Task DownloadAndExtractRelease(string zipUrl, string destinationFolder, string fileName, ProgressBar progressBar)
        {
            string tempZipFile = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                using HttpClient client = new HttpClient();

                // Запрос на скачивание
                using var response = await client.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // Общий размер файла
                long? totalBytes = response.Content.Headers.ContentLength;

                if (totalBytes.HasValue)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        progressBar.Maximum = totalBytes.Value;
                        progressBar.Value = 0;
                    });
                }

                // Скачивание файла
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempZipFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    long totalRead = 0;

                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes.HasValue)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = totalRead;
                            });
                        }
                    }
                }

                // Убедитесь, что целевая папка существует
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                // Распаковка архива
                try
                {
                    ZipFile.ExtractToDirectory(tempZipFile, destinationFolder, overwriteFiles: true);
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"Ошибка распаковки: {ioEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Удаление временного файла
                try
                {
                    File.Delete(tempZipFile);
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"Не удалось удалить временный файл: {ioEx.Message}", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Уведомление об успешной установке
                MessageBox.Show($"Файл {fileName} успешно загружен и распакован.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке или распаковке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = 0;
                });
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
            string versionFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameVersionCore", selectedVersion.Name, $"voxelcore.{selectedVersion.Name.Substring(1)}_win64");

            eB_Play.Content = Directory.Exists(versionFolder) ? "Играть" : "Установить";
            eB_Play.IsEnabled = true;
        }
    }
}

