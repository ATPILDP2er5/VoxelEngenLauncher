using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VoxelEngenLauncher;
using static VoxelEngenLauncher.App;

namespace VoxelEngenLauncher.Layouts.WindowTab
{
    public partial class LoadingTab : Window
    {
        public LoadingTab()
        {
            InitializeComponent();
            nePb_LoadingW.Minimum = 0;

            Thread thread = new Thread(() =>
            {
                bool result = LoadReleases().GetAwaiter().GetResult();
                Dispatcher.Invoke(() =>
                {
                    DialogResult = result; // Закрываем окно с результатом
                    Close();
                });
            });
            thread.Start();
        }

        /// <summary>
        /// Загружает релизы и возвращает true, если загрузка успешна, иначе false.
        /// </summary>
        private async Task<bool> LoadReleases()
        {
            string apiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases";

            try
            {
                if (!IsInternetAvailable())
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Нет подключения к сети, будут представлены только установленные версии",
                            "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    });
                    LoadLocalVersions();
                    return true;
                }

                // Загружаем релизы из GitHub
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
                                Name = $"{release.Name} (ORIG)",
                                PublishedAt = release.PublishedAt,
                                TagName = release.Name.Substring(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка при загрузке релизов: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }

            // Загружаем локальные версии
            LoadLocalVersions();
            return true;
        }

        /// <summary>
        /// Проверяет доступность интернета.
        /// </summary>
        private bool IsInternetAvailable()
        {
            try
            {
                using var ping = new Ping();
                PingReply reply = ping.Send("www.google.com", 3000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Загружает локальные версии, включая кастомные форки, из папки GameVersionCore.
        /// </summary>
        private void LoadLocalVersions()
        {
            Dispatcher.Invoke(() => neTb_PlsWait.Text = "Загрузка локальных версий...");
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameVersionCore");

            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Локальные версии отсутствуют.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                });
                return;
            }

            string[] directories = Directory.GetDirectories(localPath);
            Dispatcher.Invoke(() => nePb_LoadingW.Maximum = directories.Length);

            for (int i = 0; i < directories.Length; i++)
            {
               
                string Version = Path.GetFileName(directories[i]);
                string subRegex = Version.Substring(1).Replace(".", @"\.");
                foreach (string SubPath in Directory.GetDirectories(directories[i]))
                {                    
                    Regex regex = new Regex(@$"voxelcore.{subRegex}_win64");
                    string PathGame = Path.Combine(SubPath, "VoxelCore.exe");
                    if (regex.IsMatch(SubPath)&&!VersionControl.Any(c => c.TagName == Version.Substring(1)))
                    {
                        VersionControl.Add(new GitHubRelease
                        {
                            Name = $"{Version} (ORIG)",
                            PublishedAt = Directory.GetCreationTime(SubPath),
                            TagName = Version,
                            PathGame = PathGame
                        });
                        break;
                    }
                    else if (VersionControl.Any(c => c.TagName == Version.Substring(1)))
                    {
                        // Переменная c используется внутри фильтра, но нам нужно получить объект
                        VersionControl.FirstOrDefault(c => c.TagName == Version.Substring(1)).PathGame = PathGame;
                    }

                    else if (File.Exists(PathGame))
                    {
                        VersionControl.Add(new GitHubRelease
                        {
                            Name = $"{Path.GetFileName(SubPath)}",
                            PublishedAt = Directory.GetCreationTime(SubPath),
                            TagName = Version,
                            PathGame = PathGame
                        });
                    }
               
                }

                // Обновляем прогресс-бар
                Dispatcher.Invoke(() => nePb_LoadingW.Value = i + 1);
            }
        }

        /// <summary>
        /// Проверяет существование файла по URL.
        /// </summary>
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
                return false;
            }
        }

        /// <summary>
        /// Загружает релизы из GitHub с отображением прогресса.
        /// </summary>
        public async Task<List<GitHubRelease>> GetReleasesWithProgressAsync(string url)
        {
            Dispatcher.Invoke(() => neTb_PlsWait.Text = $"Идет проверка релизов с {url}");
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "WPF App");

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<GitHubRelease>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<GitHubRelease>();
        }
    }
}


