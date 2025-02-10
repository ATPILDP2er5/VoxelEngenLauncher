using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tomlyn.Model;
using Tomlyn;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace VoxelEngenLauncherRepack.Layouts
{
    /// <summary>
    /// Логика взаимодействия для SettingsTab.xaml
    /// </summary>
    public partial class SettingsTab : UserControl
    {
        public static ClassLang[] Languages;
        public SettingsTab()
        {
            InitializeComponent();
            var JSONLanguages = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource\\Language_dictionary\\langs.json"));
            Languages = JsonConvert.DeserializeObject<ClassLang[]>(JSONLanguages);
            List<string> dLang = new();
            foreach (var item in Languages)
            {
                dLang.Add(item.Name);
            }
            eCB_Language.ItemsSource = dLang;
            eCB_LanguageApp.ItemsSource = dLang;
            string settings = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource\\User\\Default\\settings_app.toml"));
            TomlTable tomlAppSettings;
            try
            {
                tomlAppSettings = Toml.Parse(settings).ToModel();
                var audio = tomlAppSettings["local_data"] as TomlTable;
                eCB_LanguageApp.SelectedIndex = SettingsTabHelpers.GetIndexLang(audio["lang"].ToString());
            }
            catch
            {
                eCB_LanguageApp.SelectedItem = "English";
            }
            LoadSettingsIntoGrid();
        }
        public class ClassLang
        {
            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

        }

        private void LoadSettingsIntoGrid()
        {
            string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource\\User\\Settings\\SettingsGame.toml");
            string defaultSettingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource\\User\\Default\\settings.toml");

            // Проверяем и создаем директории, если их нет
            string settingsDir = System.IO.Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            string defaultSettingsDir = System.IO.Path.GetDirectoryName(defaultSettingsPath);
            if (!Directory.Exists(defaultSettingsDir))
            {
                Directory.CreateDirectory(defaultSettingsDir);
            }

            // Проверяем наличие файла настроек
            if (!File.Exists(settingsPath))
            {
                // Если пользовательский settings.toml отсутствует, копируем из стандартного
                if (File.Exists(defaultSettingsPath))
                {
                    File.Copy(defaultSettingsPath, settingsPath);
                }
                else
                {
                    MessageBox.Show($"{Application.Current.TryFindResource("ErrorNotFoundSet") as string}", $"{Application.Current.TryFindResource("Error") as string}", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Читаем содержимое settings.toml
            TomlTable tomlSettings;
            try
            {
                string settings = File.ReadAllText(settingsPath);
                tomlSettings = Toml.Parse(settings).ToModel();
            }
            catch
            {
                // Если основной settings.toml поврежден, загружаем стандартный
                if (File.Exists(defaultSettingsPath))
                {
                    string defaultSettings = File.ReadAllText(defaultSettingsPath);
                    tomlSettings = Toml.Parse(defaultSettings).ToModel();
                    File.Copy(defaultSettingsPath, settingsPath, true);
                }
                else
                {
                    MessageBox.Show("Ошибка при загрузке файла настроек.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // --- Обработка параметров ---

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


        private void eCB_LanguageApp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string langFile = $"Resource/Language_dictionary/lang.{Languages[eCB_LanguageApp.SelectedIndex].Key}.xaml";
            ResourceDictionary newLang;
            try
            {
                newLang = new ResourceDictionary { Source = new Uri(langFile, UriKind.Relative) };
            }
            catch
            {
                newLang = new ResourceDictionary { Source = new Uri("Resource/Language_dictionary/lang.en_US.xaml", UriKind.Relative) };
            }
            // Очищаем старую локализацию и загружаем новую
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(newLang);
        }

        private void eB_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveSettingsFromGrid();
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
                ["volume-master"] = eS_GlobalVolume.Value,
                ["volume-regular"] = eS_RegularVolume.Value,
                ["volume-ui"] = eS_UIVolume.Value,
                ["volume-ambient"] = eS_AmbientVolume.Value,
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
                string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource\\User\\Settings\\SettingsGame.toml");
                File.Delete(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource\\Data\\GlobalSettings.toml"));
                var tomlMain = Toml.FromModel(tomlSettings);
                File.WriteAllText(settingsPath, tomlMain);
                MessageBox.Show("Настройки успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
