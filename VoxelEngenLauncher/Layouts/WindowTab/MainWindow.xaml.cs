using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Tomlyn;
// Документация по классу Toml: https://github.com/xoofx/Tomlyn
using Tomlyn.Model;
using VoxelEngenLauncher.Resurces.Skripts;

namespace VoxelEngenLauncher
{
    public partial class MainWindow : Window
    {
        // Поля и свойства
        private List<string?> ListVersion = new();
        public static string gameVersionCorePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameVersionCore");
        public static ClassLang[] Languages;
        public static List<Versionsetting> dVersions;
        // Конструктор
        public MainWindow()
        {
            InitializeComponent();
        }

        // Обработчики событий
        private void Window_Activated(object sender, EventArgs e)
        {
            if (Languages == null) // Чтобы избежать дублирования
            {
                foreach (var item in App.VersionControl)
                {
                    ListVersion.Add(item.Name);
                }
                eCB_ControlVershion.ItemsSource = ListVersion;
                var JSONLanguages = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\langs.json"));
                Languages = JsonConvert.DeserializeObject<ClassLang[]>(JSONLanguages);
                List<string> dLang = new();
                foreach (var item in Languages)
                {
                    dLang.Add(item.Name);
                }
                eCB_Language.ItemsSource = dLang;
                eCB_LanguageApp.ItemsSource = dLang;
 

                string settings = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\appSettings.toml"));
                TomlTable tomlAppSettings;
                try
                {
                    tomlAppSettings = Toml.Parse(settings).ToModel();
                    var audio = tomlAppSettings["startapp"] as TomlTable;
                    eCB_LanguageApp.SelectedIndex = GetIndexLang(audio["language"].ToString());
                }
                catch
                {
                    eCB_LanguageApp.SelectedItem = "English";
                }

               
            }

        }

        public class Versionsetting
        {
            public string settings_version { get; set; }
            public string latest_supported_application { get; set; }
        }



        public class ClassLang
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

        }
        public int GetIndexLang(string key)
        {
            int index = 0;
            for (int i = 0; i < Languages.Length; i++)
            {
                if (Languages[i].Key == key)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }


        private async void eB_Play_Click(object sender, RoutedEventArgs e)
        {
            if (eCB_ControlVershion.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите версию игры!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedVersion = App.VersionControl[eCB_ControlVershion.SelectedIndex];

            if (eB_Play.Content.ToString() == "Установить")
            {
                if (selectedVersion.PathGame != null)
                {
                    MessageBox.Show("Выбранная версия уже установлена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string versionFolder = Path.Combine(gameVersionCorePath, selectedVersion.Name.Split(' ')[0]);
                string fileName = $"voxelcore.{selectedVersion.TagName}_win64.zip";
                string zipUrl = $"https://github.com/{App.repoOwner}/{App.repoName}/releases/download/{selectedVersion.Name.Split(' ')[0]}/{fileName}";

                eB_Play.IsEnabled = false;

                try
                {
                    await DownloadAndExtractRelease(zipUrl, versionFolder, fileName, nePB_DownloadElement);
                    MessageBox.Show($"Версия {selectedVersion.Name} успешно установлена!");
                    App.VersionControl[eCB_ControlVershion.SelectedIndex].PathGame = Path.Combine(versionFolder, $"voxelcore.{selectedVersion.TagName}_win64", "VoxelCore.exe");
                    eB_Play.Content = "Играть";
                    eB_FolderGame.IsEnabled = true;
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
                if (selectedVersion.PathGame == null)
                {
                    MessageBox.Show("Выбранная версия игры не найдена на локальном диске.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string settingsPath = Path.Combine(Path.GetDirectoryName(selectedVersion.PathGame), "settings.toml");
                if (File.Exists(settingsPath))
                {
                    File.Delete(settingsPath);
                }

                string rootSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\GlobalSettings.toml");
                File.Copy(rootSettingsPath, settingsPath);

                if (!File.Exists(selectedVersion.PathGame))
                {
                    MessageBox.Show($"Файл VoxelCore.exe не найден: {selectedVersion.PathGame}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await LaunchVoxelCoreAsync(selectedVersion.PathGame, settingsPath);

            }
        }

        private void eCB_ControlVershion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (eCB_ControlVershion.SelectedIndex == -1)
                return;

            var selectedVersion = App.VersionControl[eCB_ControlVershion.SelectedIndex];
            eB_Play.Content = Directory.Exists(Path.GetDirectoryName(selectedVersion.PathGame)) ? "Играть" : "Установить";
            eB_Play.IsEnabled = true;
            eB_FolderGame.IsEnabled = Directory.Exists(Path.GetDirectoryName(selectedVersion.PathGame));
        }

        private void eB_Settings_Click(object sender, RoutedEventArgs e)
        {
            LoadSettingsIntoGrid();
            SettigsGrid.Visibility = Visibility.Visible;
        }

        private void eB_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveSettingsFromGrid();
        }

        private void eB_FolderGame_Click(object sender, RoutedEventArgs e)
        {
            if (eCB_ControlVershion.SelectedIndex == -1)
            {
                MessageBox.Show("Пожалуйста, выберите версию игры.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedVersion = App.VersionControl[eCB_ControlVershion.SelectedIndex];
            if (File.Exists(selectedVersion.PathGame))
            {
                Process.Start("explorer.exe", Path.GetDirectoryName(selectedVersion.PathGame));
            }
            else
            {
                MessageBox.Show("Выбранная версия игры не найдена на локальном диске.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void eB_Close_Click(object sender, RoutedEventArgs e)
        {
            SettigsGrid.Visibility = Visibility.Hidden;
        }

        private void OpenTBI_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        // Общие методы
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
                SettingsManager.UpdateGlobalSettings(settingPath); // Add missing semicolon
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске VoxelCore.exe: {ex.Message}");
            }
        }

        private void LoadSettingsIntoGrid()
        {
            // Читаем содержимое settings.toml
            string settings = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\GlobalSettings.toml"));
            TomlTable tomlSettings;
            try
            {
                tomlSettings = Toml.Parse(settings).ToModel();
            }
            catch
            {

                string rootSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\DefaultSettings_ReadOnly.toml");
                tomlSettings = Toml.Parse(rootSettingsPath).ToModel();
                File.Copy(rootSettingsPath, settings);
            }

            // Аудио
            var audio = tomlSettings["audio"] as TomlTable;
            if (audio != null)
            {
                eS_GlobalVolume.Value = Convert.ToDouble(audio["volume-master"]);
                eS_RegularVolume.Value = Convert.ToDouble(audio["volume-regular"]);
                eS_UIVolume.Value = Convert.ToDouble(audio["volume-ui"]);
                eS_AmbientVolume.Value = Convert.ToDouble(audio["volume-ambient"]);
                eS_MusicVolume.Value = Convert.ToDouble(audio["volume-music"]);
            }

            // Экран
            var display = tomlSettings["display"] as TomlTable;
            if (display != null)
            {
                eETB_WidthWindow.Text = display["width"].ToString();
                eETB_HeightWindow.Text = display["height"].ToString();
                eS_FPS_Limit.Value = Convert.ToDouble(display["framerate"]);
                eCkB_WindowMod.IsChecked = Convert.ToBoolean(display["fullscreen"]);
                eCkB_MinimFPSLimitet.IsChecked = Convert.ToBoolean(display["limit-fps-iconified"]);
            }

            // Камера
            var camera = tomlSettings["camera"] as TomlTable;
            if (camera != null)
            {
                eS_Sensitiv.Value = Convert.ToDouble(camera["sensitivity"]);
                eEB_FOV.Value = Convert.ToInt32(camera["fov"].ToString());
                eCkB_EnableFOVEffects.IsChecked = Convert.ToBoolean(camera["fov-effects"]);
                eCkB_EnableShake.IsChecked = Convert.ToBoolean(camera["shaking"]);
                eChB_EnableInertia.IsChecked = Convert.ToBoolean(camera["inertia"]);
            }

            // Чанки
            var chunks = tomlSettings["chunks"] as TomlTable;
            if (chunks != null)
            {
                eS_DistanceLoad.Value = Convert.ToDouble(chunks["load-distance"]);
                eS_SpeadLoad.Value = Convert.ToDouble(chunks["load-speed"]);
            }

            // Графика
            var graphics = tomlSettings["graphics"] as TomlTable;
            if (graphics != null)
            {

            }

            // UI
            var ui = tomlSettings["ui"] as TomlTable;
            if (ui != null)
            {
                for (int i = 0; i < Languages.Length; i++)
                {
                    if (Languages[i].Key == ui["language"].ToString())
                    {
                        eCB_Language.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Отладка
            var debug = tomlSettings["debug"] as TomlTable;
            if (debug != null)
            {
                eCkB_TestMod.IsChecked = Convert.ToBoolean(debug["generator-test-mode"]);
                eCkB_WLights.IsChecked = Convert.ToBoolean(debug["do-write-lights"]);
            }
        }


        private void SaveSettingsFromGrid()
        {

            // Создаём объект для хранения настроек
            var tomlSettings = new TomlTable();
            //tomlSettings["version"] = lastVersionStart;
            // Секция Audio
            var audio = new TomlTable
            {
                ["enabled"] = ChB_Enable.IsChecked, // Если есть CheckBox для включения звука, добавьте его проверку
                ["volume-master"] = eS_GlobalVolume.Value ,
                ["volume-regular"] = eS_RegularVolume.Value ,
                ["volume-ui"] = eS_UIVolume.Value ,
                ["volume-ambient"] = eS_AmbientVolume.Value ,
                ["volume-music"] = eS_MusicVolume.Value 
            };
            tomlSettings["audio"] = audio;

            // Секция Display
            var display = new TomlTable
            {
                ["width"] = int.TryParse(eETB_WidthWindow.Text, out int width) ? width : 1280,
                ["height"] = int.TryParse(eETB_HeightWindow.Text, out int height) ? height : 720,
                ["samples"] = 0, // Если добавите TextBox для "samples", замените на его значение
                ["framerate"] = (int)eS_FPS_Limit.Value,
                ["fullscreen"] = eCkB_WindowMod.IsChecked ?? false,
                ["limit-fps-iconified"] = eCkB_MinimFPSLimitet.IsChecked ?? false
            };
            tomlSettings["display"] = display;

            // Секция Camera
            var camera = new TomlTable
            {
                ["sensitivity"] = eS_Sensitiv.Value,
                ["fov"] = (int)eEB_FOV.Value,
                ["fov-effects"] = eCkB_EnableFOVEffects.IsChecked ?? true,
                ["shaking"] = eCkB_EnableShake.IsChecked ?? true,
                ["inertia"] = eChB_EnableInertia.IsChecked ?? true
            };
            tomlSettings["camera"] = camera;

            // Секция Chunks
            var chunks = new TomlTable
            {
                ["load-distance"] = (int)eS_DistanceLoad.Value,
                ["load-speed"] = (int)eS_SpeadLoad.Value,
                ["padding"] = 2
            };
            tomlSettings["chunks"] = chunks;

            // Секция Graphics
            var graphics = new TomlTable
            {
                ["fog-curve"] = (double)eS_Fog.Value,
                ["backlight"] = eChB_EnableBlacklight.IsChecked,
                ["gamma"] = (double)eS_Gamma.Value,
                ["frustum-culling"] = eCH_EFC.IsChecked,
                ["skybox-resolution"] = 96,
                ["chunk-max-vertices"] = 200000,
                ["chunk-max-renderers"] = 6
            };
            tomlSettings["graphics"] = graphics;

            // Секция UI
            var ui = new TomlTable
            {
                ["language"] = Languages[eCB_Language.SelectedIndex].Key ?? "ru_RU",
                ["world-preview-size"] = 64 // Если добавите TextBox для "world-preview-size", замените на его значение
            };
            tomlSettings["ui"] = ui;

            // Секция Debug
            var debug = new TomlTable
            {
                ["generator-test-mode"] = eCkB_TestMod.IsChecked ?? false,
                ["do-write-lights"] = eCkB_WLights.IsChecked ?? true
            };
            tomlSettings["debug"] = debug;

            // Сохраняем в файл settings.toml
            try
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\GlobalSettings.toml"));
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\GlobalSettings.toml");
                var tomlMain = Toml.FromModel(tomlSettings);
                File.WriteAllText(settingsPath, tomlMain);
                MessageBox.Show("Настройки успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ChangeLanguage(string culture)
        {
            string langFile = $"Resurces/Localization/lang.{culture}.xaml";

            ResourceDictionary newLang = new ResourceDictionary { Source = new Uri(langFile, UriKind.Relative) };

            // Очищаем старую локализацию и загружаем новую
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(newLang);
            
        }

        private void eCB_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            { 
                ChangeLanguage(Languages[eCB_LanguageApp.SelectedIndex].Key); 
            }
            catch
            {
                ChangeLanguage("en_US");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var tomlAppSettings = new TomlTable();
            tomlAppSettings["startapp"] = new TomlTable
            {
                ["language"] = Languages[eCB_LanguageApp.SelectedIndex].Key ?? "en_US"
            };
            try
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\appSettings.toml"));
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\appSettings.toml");
                var tomlMain = Toml.FromModel(tomlAppSettings);
                File.WriteAllText(settingsPath, tomlMain);
            }
            catch
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\appSettings.toml");
                var tomlMain = Toml.FromModel(tomlAppSettings);
                File.WriteAllText(settingsPath, tomlMain);
            }
        }
    }
}
