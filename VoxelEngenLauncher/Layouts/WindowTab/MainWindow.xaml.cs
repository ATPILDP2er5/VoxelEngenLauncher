﻿using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Tomlyn;
using Tomlyn.Model;

namespace VoxelEngenLauncher
{
    public partial class MainWindow : Window
    {
        private List<string?> ListVersion = new();
        public static string gameVersionCorePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameVersionCore");
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

            // Название версии и тег ядра
            string versionName = selectedVersion.Name;
            string versionTag = selectedVersion.TagName; // Пример: "0.25.3"

           

            // Ищем папку, содержащую выбранную версию
            string[] potentialFolders = Directory.GetDirectories(gameVersionCorePath, "*", SearchOption.TopDirectoryOnly);
            string targetFolder = null;

            foreach (var folder in potentialFolders)
            {
                // Проверяем, содержит ли папка файл voxelcore.[версия]_win64
                string expectedFilePath = Path.Combine(folder, $"voxelcore.{versionTag}_win64");

                if (Directory.Exists(folder) && File.Exists(Path.Combine(expectedFilePath, "VoxelCore.exe")))
                {
                    targetFolder = folder;
                    break;
                }
            }

            if (eB_Play.Content.ToString() == "Установить")
            {
                if (targetFolder != null)
                {
                    MessageBox.Show("Выбранная версия уже установлена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string versionFolder = Path.Combine(gameVersionCorePath, versionName);
                string fileName = $"voxelcore.{versionTag}_win64.zip";
                selectedVersion.ZipUrl = $"https://github.com/{App.repoOwner}/{App.repoName}/releases/download/{selectedVersion.Name}/{fileName}";

                eB_Play.IsEnabled = false;

                try
                {
                    // Загружаем и распаковываем релиз
                    await DownloadAndExtractRelease(selectedVersion.ZipUrl, versionFolder, fileName, nePB_DownloadElement);
                    MessageBox.Show($"Версия {selectedVersion.Name} успешно установлена!");
                    eB_Play.Content = "Играть";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при установке версии: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    eB_Play.IsEnabled = true;
                }
            }
            else if (eB_Play.Content.ToString() == "Играть")
            {
                if (targetFolder == null)
                {
                    MessageBox.Show("Выбранная версия игры не найдена на локальном диске.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string settingsPath = Path.Combine(targetFolder, $"voxelcore.{versionTag}_win64", "settings.toml");
                if (File.Exists(settingsPath))
                {
                    // Удаляем файл, если он уже существует
                    File.Delete(settingsPath);
                }
                string RootSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.toml");
                File.Move(RootSettingsPath, settingsPath);

                // Путь до исполняемого файла
                string exePath = Path.Combine(targetFolder, $"voxelcore.{versionTag}_win64", "VoxelCore.exe");

                if (!File.Exists(exePath))
                {
                    MessageBox.Show($"Файл VoxelCore.exe не найден: {exePath}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await LaunchVoxelCoreAsync(exePath, settingsPath);
            }
        }


        public static async Task DownloadAndExtractRelease(string zipUrl, string destinationFolder, string fileName, ProgressBar progressBar)
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



        private static async Task LaunchVoxelCoreAsync(string exePath, string settingPath)
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
                string RootSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.toml");
                if (File.Exists(RootSettingsPath))
                {
                    // Удаляем файл, если он уже существует
                    File.Delete(RootSettingsPath);
                }
                File.Move(settingPath, RootSettingsPath);
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
            string versionFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameVersionCore", selectedVersion.Name.Split(' ')[0], $"voxelcore.{selectedVersion.TagName}_win64");

            eB_Play.Content = Directory.Exists(versionFolder) ? "Играть" : "Установить";
            eB_Play.IsEnabled = true;
        }

        private void eB_Settings_Click(object sender, RoutedEventArgs e)
        {
            LoadSettingsIntoGrid(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.toml"));
            if (Resources["SettingsGrid"] is Grid settingsGrid)
            {
                // Копируем содержимое из SettingsGrid в MainGrid
                MainGrid.Children.Clear(); // Очищаем MainGrid, если нужно
                MainGrid.Children.Add(settingsGrid);
            }
            else
            {
                MessageBox.Show("Ресурс SettingsGrid не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Загружает настройки из settings.toml в элементы интерфейса (Grid).
        /// </summary>
        /// <param name="filePath">Путь к файлу settings.toml.</param>
        private void LoadSettingsIntoGrid(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Файл settings.toml не найден. Будет создан новый файл.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                File.WriteAllText(filePath, "");
            }

            try
            {
                // Читаем содержимое TOML
                string tomlContent = File.ReadAllText(filePath);
                var tomlData = Toml.Parse(tomlContent).ToModel();

                // Получаем Grid из ресурсов
                if (Resources["SettingsGrid"] is Grid settingsGrid && tomlData is TomlTable table)
                {
                    foreach (var child in settingsGrid.Children)
                    {
                        if (child is TabControl tabControl)
                        {
                            foreach (TabItem tab in tabControl.Items)
                            {
                                if (tab.Content is StackPanel stackPanel && table.TryGetValue(tab.Header.ToString().ToLower(), out var section))
                                {
                                    var sectionTable = section as TomlTable;

                                    foreach (var element in stackPanel.Children)
                                    {
                                        if (element is TextBox textBox)
                                        {
                                            string key = GetLabelBefore(textBox, stackPanel);
                                            if (key != null && sectionTable.TryGetValue(key, out var value))
                                            {
                                                textBox.Text = value.ToString();
                                            }
                                        }
                                        else if (element is CheckBox checkBox)
                                        {
                                            string key = checkBox.Content.ToString().ToLower().Replace(" ", "-");
                                            if (sectionTable.TryGetValue(key, out var value))
                                            {
                                                checkBox.IsChecked = Convert.ToBoolean(value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохраняет настройки из элементов интерфейса в settings.toml.
        /// </summary>
        /// <param name="filePath">Путь к файлу settings.toml.</param>
        private void SaveSettingsFromGrid(string filePath)
        {
            var tomlData = new TomlTable();

            if (Resources["SettingsGrid"] is Grid settingsGrid)
            {
                foreach (var child in settingsGrid.Children)
                {
                    if (child is TabControl tabControl)
                    {
                        foreach (TabItem tab in tabControl.Items)
                        {
                            if (tab.Content is StackPanel stackPanel)
                            {
                                var sectionTable = new TomlTable();
                                string sectionName = tab.Header.ToString().ToLower();

                                foreach (var element in stackPanel.Children)
                                {
                                    if (element is TextBox textBox)
                                    {
                                        string key = GetLabelBefore(textBox, stackPanel);
                                        if (key != null)
                                        {
                                            sectionTable[key] = textBox.Text;
                                        }
                                    }
                                    else if (element is CheckBox checkBox)
                                    {
                                        string key = checkBox.Content.ToString().ToLower().Replace(" ", "-");
                                        sectionTable[key] = checkBox.IsChecked ?? false;
                                    }
                                }

                                tomlData[sectionName] = sectionTable;
                            }
                        }
                    }
                }
            }

            try
            {
                // Сохраняем в файл
                File.WriteAllText(filePath, Toml.FromModel(tomlData));
                MessageBox.Show("Настройки успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Получает ключ из Label перед TextBox в StackPanel.
        /// </summary>
        private static string GetLabelBefore(TextBox textBox, StackPanel stackPanel)
        {
            int index = stackPanel.Children.IndexOf(textBox);
            if (index > 0 && stackPanel.Children[index - 1] is Label label)
            {
                return label.Content.ToString().ToLower().Replace(" ", "-");
            }
            return null;
        }

        private void eB_Save_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем настройки в файл
            SaveSettingsFromGrid(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.toml"));
        }

        private void eB_FolderGame_Click(object sender, RoutedEventArgs e)
        {
            if (eCB_ControlVershion.SelectedIndex == -1)
            {
                MessageBox.Show("Пожалуйста, выберите версию игры.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получаем выбранную версию из списка
            var selectedVersion = App.VersionControl[eCB_ControlVershion.SelectedIndex];

            // Извлекаем название версии (возможно с кастомным именем форка)
            string versionName = selectedVersion.Name;
            string versionTag = selectedVersion.TagName; // Версия ядра (например, "0.25.3")

            // Папка с версиями игры
            string gameVersionCorePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameVersionCore");

            // Ищем папку, содержащую выбранную версию
            string[] potentialFolders = Directory.GetDirectories(gameVersionCorePath, "*", SearchOption.TopDirectoryOnly);
            string targetFolder = null;

            foreach (var folder in potentialFolders)
            {
                // Проверяем, содержит ли папка нужный файл voxelcore.[версия]_win64
                string expectedFilePath = Path.Combine(folder, $"voxelcore.{versionTag}_win64");

                if (File.Exists(Path.Combine(expectedFilePath, "VoxelCore.exe")))
                {
                    targetFolder = folder;
                    break;
                }
            }

            if (targetFolder != null)
            {
                Process.Start("explorer.exe", targetFolder);
            }
            else
            {
                MessageBox.Show("Выбранная версия игры не найдена на локальном диске.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void eB_Close_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Children.Clear();
        }
    }
}


