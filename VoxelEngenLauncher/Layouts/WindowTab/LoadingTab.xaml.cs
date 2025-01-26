using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VoxelEngenLauncher;
using VoxelEngenLauncher.Skripts;
using static VoxelEngenLauncher.App;

namespace VoxelEngenLauncher.Layouts.WindowTab
{
    /// <summary>
    /// Логика взаимодействия для LoadingTab.xaml
    /// </summary>
    public partial class LoadingTab : Window
    {
        public LoadingTab()
        {
            InitializeComponent();
            Thread thread = new Thread(() =>
            {
                GetAccounts.GetAccoutsFromLocal();
                bool result = LoadReleases().GetAwaiter().GetResult();
                Dispatcher.Invoke(() =>
                {
                    // Закрываем окно с результатом
                    DialogResult = result;
                    Close();
                });
            });
            thread.Start();
        }

        /// <summary>
        /// Загружает релизы и возвращает true, если загрузка успешна, иначе false.
        /// </summary>
        /// <returns>True при успешной загрузке, иначе false.</returns>
        private async Task<bool> LoadReleases()
        {
            // Загружаем локальные версии (включая форки)
            LoadLocalVersions();

            // Если локальные версии найдены, они отображаются в интерфейсе
            if (VersionControl.Count > 0)
            {
                return true; // Локальные версии успешно загружены
            }

            string apiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases";

            try
            {
                // Пытаемся загрузить релизы из GitHub
                List<GitHubRelease> releases = await GetReleasesWithProgressAsync(apiUrl);

                if (releases != null && releases.Count > 0)
                {
                    foreach (var release in releases)
                    {
                        string versionTag = release.Name.TrimStart('v');
                        string fileName = $"voxelcore.{versionTag}_win64.zip";
                        string fileUrl = $"https://github.com/{repoOwner}/{repoName}/releases/download/{release.Name}/{fileName}";

                        // Проверяем существование файла
                        if (await CheckFileExistsAsync(fileUrl))
                        {
                            VersionControl.Add(new GitHubRelease
                            {
                                Name = release.Name,
                                HtmlUrl = release.HtmlUrl,
                                PublishedAt = release.PublishedAt,
                                TagName = release.Name.Substring(1)
                            });
                        }
                    }

                    return true; // Успешная загрузка
                }

                return false; // Релизы отсутствуют
            }
            catch
            {
                // В случае ошибки проверяем только локальные версии
                return VersionControl.Count > 0;
            }
        }


        /// <summary>
        /// Проверяет существование файла по URL.
        /// </summary>
        /// <param name="fileUrl"></param>
        /// <returns></returns>
        private async Task<bool> CheckFileExistsAsync(string fileUrl)
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "WPF App");

                HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, fileUrl));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false; // Файл недоступен или произошла ошибка
            }
        }

        /// <summary>
        /// Загружает релизы из GitHub с отображением прогресса.
        /// </summary>
        private async Task<List<GitHubRelease>> GetReleasesWithProgressAsync(string url)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "WPF App");

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (releases == null || releases.Count == 0)
            {
                return new List<GitHubRelease>();
            }

            // Обновляем прогресс-бар
            Dispatcher.Invoke(() =>
            {
                nePb_LoadingW.Minimum = 0;
                nePb_LoadingW.Maximum = releases.Count;
                nePb_LoadingW.Value = 0;
            });

            for (int i = 0; i < releases.Count; i++)
            {
                // Обновляем прогресс-бар
                Dispatcher.Invoke(() => nePb_LoadingW.Value = i + 1);

                // Задержка для эмуляции
                await Task.Delay(200);
            }

            return releases;
        }

        /// <summary>
        /// Загружает локальные версии, включая кастомные форки, из папки GameVersionCore.
        /// </summary>
        private void LoadLocalVersions()
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameVersionCore");

            if (!Directory.Exists(localPath))
            {
                return; // Папка GameVersionCore отсутствует
            }

            // Обходим все директории внутри GameVersionCore
            string[] directories = Directory.GetDirectories(localPath);

            foreach (var dir in directories)
            {
                // Ищем файлы формата voxelcore.[версия]_win64.zip
                string[] versionFiles = Directory.GetFiles(dir, "voxelcore.*_win64");

                foreach (var file in versionFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string version = fileName.Remove(1, "voxelcore.".Length);
                    {
                        string versionTag = version.Substring(0, fileName.IndexOf('_')); // Извлекаем версию из имени файла
                        string forkName = new DirectoryInfo(dir).Name; // Имя директории = имя форка

                        VersionControl.Add(new GitHubRelease
                        {
                            Name = $"(v{versionTag}) {forkName}",
                            HtmlUrl = file,
                            PublishedAt = DateTime.Now, // Используем текущую дату для локальных версий
                            TagName = versionTag
                        });
                    }
                }
            }
        }

    }
}


