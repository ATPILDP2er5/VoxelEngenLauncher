using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using VoxelEngenLauncher.Resurces.Skripts;
using VoxelEngenLauncherRepack.Resource.Scripts;

namespace VoxelEngenLauncherRepack.Layouts
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static List<string[]> listForks = new List<string[]>();
        public static List<string> ForksName = new List<string>();
        public static string GameDirPath = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            listForks = GetForksList();
            foreach (var t in listForks)
            {
                ForksName.Add($"({t[0]}) - {t[1]}");
            }
            eCB_ControlVersion.ItemsSource = ForksName;

        }
        static async Task<bool> StartExternalApp(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath))
            {
                Console.WriteLine("Ошибка: путь к файлу не указан.");
                return false;
            }

            if (!File.Exists(appPath))
            {
                Console.WriteLine($"Ошибка: файл не найден ({appPath})");
                return false;
            }

            string WorkingDirectoryQQ = System.IO.Path.GetDirectoryName(appPath);
            if (WorkingDirectoryQQ == null)
            {
                Console.WriteLine("Ошибка: не удалось определить рабочую директорию.");
                return false;
            }

            try
            {
                // Пути к файлам настроек
                string appSettingsPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resource\\User\\Settings",
                    "SettingsGame.toml"
                );
                string gameSettingsPath = System.IO.Path.Combine(WorkingDirectoryQQ, "settings.toml");

                // Копирование глобальных настроек в папку игры
                if (File.Exists(appSettingsPath))
                {
                    File.Copy(appSettingsPath, gameSettingsPath, overwrite: true);
                }

                // Запуск игры и ожидание завершеия
                Process gameProcess = new()
                {
                    StartInfo =
                    {
                        FileName = appPath,
                        UseShellExecute = true,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(appPath)
                    }
                };
                gameProcess.Start();
                await gameProcess.WaitForExitAsync(); // Асинхронное ожидание

                    // Обновление глобальных настроек
                    SettingsManager.UpdateGlobalSettings(gameSettingsPath);
                

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }
        static List<string[]> GetForksList()
        {
            string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Data", "Forks");
            List<string[]> forksList = new List<string[]>();

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            foreach (var parentDir in Directory.GetDirectories(basePath))
            {
                string parentName = new DirectoryInfo(parentDir).Name;

                foreach (var childDir in Directory.GetDirectories(parentDir))
                {
                    string childName = new DirectoryInfo(childDir).Name;
                    forksList.Add(new string[] { parentName, childName, Directory.GetDirectories(childDir)[0] });
                }
            }

            return forksList;
        }

        private void eB_AddForkG_Click(object sender, RoutedEventArgs e)
        {
            CreateForkTab.Visibility = Visibility.Visible;
            ProfileTab.Visibility = Visibility.Hidden;
            SettingTab.Visibility = Visibility.Hidden;
        }

        private async void eB_Play_Click(object sender, RoutedEventArgs e)
        {
            if (!await StartExternalApp(GameDirPath))
                Console.WriteLine("Ошибка: не удалось запустить приложение.");
        }

        private void eB_DelitFork_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("Вы точно Хотите удалить форк?", null, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
            }
        }

        private void eB_Settings_Click(object sender, RoutedEventArgs e)
        {
            CreateForkTab.Visibility = Visibility.Hidden;
            ProfileTab.Visibility = Visibility.Hidden;
            SettingTab.Visibility = Visibility.Visible;
        }

        private void eB_FolderGame_Click(object sender, RoutedEventArgs e)
        {
            if (eCB_ControlVersion.SelectedIndex == -1)
            {
                MessageBox.Show("Пожалуйста, выберите версию игры.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedVersion = listForks[eCB_ControlVersion.SelectedIndex][2];
            if (System.IO.Path.Exists(selectedVersion))
            {
                Process.Start("explorer.exe", selectedVersion);
            }
            else
            {
                MessageBox.Show("Выбранная версия игры не найдена на локальном диске.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void eB_GeimerProfile_Click(object sender, RoutedEventArgs e)
        {
            CreateForkTab.Visibility = Visibility.Hidden;
            ProfileTab.Visibility = Visibility.Visible;
            SettingTab.Visibility = Visibility.Hidden;
        }

        private void eCB_ControlVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GameDirPath = System.IO.Path.Combine(listForks[eCB_ControlVersion.SelectedIndex][2], "VoxelCore.exe");
            if (!File.Exists(GameDirPath))
                listForks.RemoveAt(eCB_ControlVersion.SelectedIndex);
            else
            {
                eB_FolderGame.IsEnabled = true;
                eB_Play.IsEnabled = true;
                eB_DelitFork.IsEnabled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void CloseGSD_Click(object sender, RoutedEventArgs e)
        {
            CreateForkTab.Visibility = Visibility.Hidden;
            ProfileTab.Visibility = Visibility.Hidden;
            SettingTab.Visibility = Visibility.Hidden;
        }
    }
}
