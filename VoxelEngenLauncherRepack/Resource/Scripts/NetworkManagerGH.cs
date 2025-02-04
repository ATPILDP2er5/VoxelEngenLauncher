using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VoxelEngenLauncherRepack.Resource.Scripts
{
    public class NetworkManagerGH
    {
        // 1. Добавлена потокобезопасная коллекция
        private static readonly SemaphoreSlim _releasesLock = new SemaphoreSlim(1, 1);

        public static string RepoOwner { get; set; } = "MihailRis";
        public static string RepoName { get; set; } = "VoxelEngine-Cpp";


        // 2. Заменено async void на async Task
        public static async Task GetReleasesAsync(
            ProgressBar progressBar,
            CancellationToken token,
            IProgress<int> progress = null)
        {
            try
            {
                // 3. Использование свойств класса вместо хардкода
                string apiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases";

                var releases = await GetReleasesDataAsync(apiUrl, token);
                if (releases == null || releases.Count == 0) return;

                progressBar.Dispatcher.Invoke(() =>
                {
                    progressBar.Minimum = 0;
                    progressBar.Maximum = releases.Count;
                });

                int processedCount = 0;
                foreach (var release in releases)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        string versionTag = release.Name?.TrimStart('v') ?? string.Empty;
                        string fileName = $"voxelcore.{versionTag}_win64.zip";
                        string fileUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases/download/{release.Name}/{fileName}";

                        if (await CheckFileExistsAsync(fileUrl, token))
                        {
                            await _releasesLock.WaitAsync(token);
                            try
                            {
                                App.Releases.Add(new GitHubRelease
                                {
                                    Name = release.Name ?? "Unnamed Release",
                                    PublishedAt = release.PublishedAt,
                                    TagName = release.TagName ?? "v0.0.0",
                                    HtmlUrl = fileUrl
                                });
                            }
                            finally
                            {
                                _releasesLock.Release();
                            }
                        }

                        processedCount++;
                        progress?.Report(processedCount);
                        progressBar.Dispatcher.Invoke(() => progressBar.Value = processedCount);
                    }
                    catch (Exception ex)
                    {
                        // Логирование ошибки для конкретного релиза
                        Console.WriteLine($"Error processing release {release?.Name}: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Корректная обработка отмены
                Console.WriteLine("Operation was canceled");
                throw;
            }
            finally
            {
                progressBar.Dispatcher.Invoke(() => progressBar.Value = progressBar.Maximum);
            }
        }

        private static async Task<List<GitHubRelease>> GetReleasesDataAsync(string url, CancellationToken token)
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                client.Timeout = TimeSpan.FromSeconds(30);

                using var response = await client.GetAsync(url, token);
                Console.WriteLine($"Fetching releases from: {url}");
                Console.WriteLine($"Response Status: {response.StatusCode}");

                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Body: {jsonResponse}");

                return JsonSerializer.Deserialize<List<GitHubRelease>>(
                    jsonResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error: {ex.Message}");
                return null;
            }
        }

        public static async Task<bool> CheckFileExistsAsync(string fileUrl, CancellationToken token)
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "WPF App");
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.SendAsync(
                    new HttpRequestMessage(HttpMethod.Head, fileUrl),
                    token
                );

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public static async Task DowloadRelease(GitHubRelease GHR, ProgressBar BG)
        {
            string fileName = $"voxelcore.{GHR.Name.Substring(1)}_win64.zip";
            // Если передан путь, используем его, иначе сохраняем в стандартную папку
            string tempDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Data", "Core");

            // Убедимся, что папка для временных файлов существует
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            string tempZipFile = Path.Combine(tempDirectory, fileName);

            try
            {
                using HttpClient client = new HttpClient();

                // Запрос на скачивание
                using var response = await client.GetAsync(GHR.HtmlUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // Общий размер файла
                long? totalBytes = response.Content.Headers.ContentLength;

                if (totalBytes.HasValue)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BG.Maximum = totalBytes.Value;
                        BG.Value = 0;
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
                                BG.Value = totalRead;
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BG.Value = 0;
                });
            }
        }
        public class GitHubRelease
        {
            public string Name { get; set; } = "Unnamed Release";
            public string TagName { get; set; } = "v0.0.0";
            public DateTime? PublishedAt { get; set; }
            public string HtmlUrl { get; set; } = string.Empty;
        }
    }
}