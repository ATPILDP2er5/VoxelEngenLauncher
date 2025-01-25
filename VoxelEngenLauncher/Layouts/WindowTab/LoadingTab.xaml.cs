using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VoxelEngenLauncher;
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
            string apiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases";

            try
            {
                // Загружаем релизы с прогрессом
                List<GitHubRelease> releases = await GetReleasesWithProgressAsync(apiUrl);

                // Проверяем, если релизы найдены
                if (releases != null && releases.Count > 0)
                {
                    foreach (var release in releases)
                    {
                        VersionControl.Add(new GitHubRelease
                        {
                            Name = release.Name,
                            HtmlUrl = release.HtmlUrl,
                            PublishedAt = release.PublishedAt,
                            TagName = release.TagName
                        });
                    }

                    return true; // Успешная загрузка
                }

                return false; // Релизы отсутствуют
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });

                return false; // Ошибка загрузки
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
    }
}


