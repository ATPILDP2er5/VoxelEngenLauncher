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
            LoadSettingsIntoGrid();
            var JSONLanguages = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurce\\Language_dictionary\\langs.json"));
            Languages = JsonConvert.DeserializeObject<ClassLang[]>(JSONLanguages);
            List<string> dLang = new();
            foreach (var item in Languages)
            {
                dLang.Add(item.Name);
            }
            eCB_Language.ItemsSource = dLang;
            eCB_LanguageApp.ItemsSource = dLang;
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
            // Читаем содержимое settings.toml
            string settings = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\GlobalSettings.toml"));
            TomlTable tomlSettings;
            try
            {
                tomlSettings = Toml.Parse(settings).ToModel();
            }
            catch
            {

                string rootSettingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resurces\\Data\\DefaultSettings_ReadOnly.toml");
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
        

        private void eCB_LanguageApp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void eB_Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void eB_Close_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
