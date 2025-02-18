using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using static VoxelEngenLauncherRepack.Resource.Scripts.NetworkManagerGH;

namespace VoxelEngenLauncherRepack.Resource.Scripts
{
    public class StorageManager
    {
        // 1. Потокобезопасная коллекция
        private static readonly ConcurrentBag<Forks> _localForks = new();
        private static readonly SemaphoreSlim _syncLock = new(1, 1);

        public class Forks
        {
            public string Name { get; set; } = "Unnamed Fork";
            public string WorkPath { get; set; } = string.Empty;
            public string CoreVersion { get; set; } = "0.0.0";
        }

        // 2. Удалены неиспользуемые классы

        public async Task FindLocalDataAsync(ProgressBar bar, CancellationToken token)
        {
            await LoadLocalForksAsync(bar, token);
        }
        public static async Task LoadLocalForksAsync(
            ProgressBar bar,
            CancellationToken token,
            IProgress<int> progress = null)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string path = System.IO.Path.Combine(appData, "VEL", "Resource", "Data", "Forks");
                //string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource/Data/Forks");
                if (!Directory.Exists(path)) return;

                var versions = Directory.GetDirectories(path).ToList();
                int totalProcessed = 0;

                await _syncLock.WaitAsync(token);
                try
                {
                    foreach (var versionDir in versions)
                    {
                        token.ThrowIfCancellationRequested();
                        string tagVersion = Path.GetFileName(versionDir);

                        var forksDirs = Directory.GetDirectories(versionDir);
                        await UpdateProgressBarAsync(bar, 0, forksDirs.Length);

                        int processed = 0;
                        foreach (var forkDir in forksDirs)
                        {
                            token.ThrowIfCancellationRequested();
                            await ProcessForkDirectoryAsync(forkDir, tagVersion);

                            processed++;
                            totalProcessed++;
                            progress?.Report(totalProcessed);
                            await UpdateProgressBarAsync(bar, processed);
                        }
                    }
                }
                finally
                {
                    _syncLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Логирование вместо MessageBox
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            finally
            {
                await ResetProgressBarAsync(bar);
            }
        }

        private static async Task ProcessForkDirectoryAsync(string forkDir, string tagVersion)
        {
            try
            {
                string jsonPath = Path.Combine(forkDir, "data.json");
                if (!File.Exists(jsonPath)) return;

                string json = await File.ReadAllTextAsync(jsonPath);
                var fork = JsonConvert.DeserializeObject<Forks>(json);

                if (fork != null)
                {
                    fork.CoreVersion = tagVersion;
                    _localForks.Add(fork);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {forkDir}: {ex.Message}");
            }
        }

        private static async Task UpdateProgressBarAsync(ProgressBar bar, int value, int? max = null)
        {
            if (bar.Dispatcher == null) return;

            await bar.Dispatcher.InvokeAsync(() =>
            {
                if (max.HasValue) bar.Maximum = max.Value;
                bar.Value = value;
            });
        }

        private static async Task ResetProgressBarAsync(ProgressBar bar)
        {
            await UpdateProgressBarAsync(bar, 0, 0);
        }

        public static async Task ExtractCoreFromZIPAsync(string Version, string CustomName)
        {
            string fileName = $"voxelcore.{Version.Substring(1)}_win64.zip";
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string tempDirectory = Path.Combine(appData, "VEL", "Resource", "Data", "Core");
            string forkDirectory = Path.Combine(appData, "VEL", "Resource", "Data", "Forks", Version, CustomName);

            if (!Directory.Exists(forkDirectory))
            {
                Directory.CreateDirectory(forkDirectory);
            }

            // Распаковка архива
            try
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(Path.Combine(tempDirectory, fileName), forkDirectory, overwriteFiles: true));
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Ошибка распаковки: {ioEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                MessageBox.Show("Ядро форка успешно распаковано.", "Успехъ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        public static async Task ExtractModsFromZIPAsync(string FinalPath, List<String> MODS)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string startDirectory = System.IO.Path.Combine(appData, "VEL", "Resource", "Data", "Mods");
            //string startDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Data", "Mods");
            string finalDirectory = Path.Combine(FinalPath, "content");
            foreach (string PMD in MODS)
            {
                string MSD = Path.Combine(startDirectory, PMD);

                if (!Directory.Exists(finalDirectory))
                {
                    Directory.CreateDirectory(finalDirectory);
                }


                await Task.Run(() => ZipFile.ExtractToDirectory(Path.Combine(startDirectory, PMD), finalDirectory, overwriteFiles: true));


            }
             
            MessageBox.Show("Data-паки успешно мигрированы в форк.", "Успехъ", MessageBoxButton.OK, MessageBoxImage.Information);
            
        }


        // 3. Публичное свойство с потокобезопасным доступом
        public static List<Forks> LocalForks => _localForks.ToList();
    }
public class GitHubRelease
    {
        public string Name { get; set; }
        public string TagName { get; set; }
        public string Body { get; set; }
        // Добавьте другие необходимые свойства
    }

    public class DataManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VEL",
            "Cache",
            "AppCN"
        );

        private static readonly string ReleasesFilePath = Path.Combine(AppDataPath, "releases.json");

        // Сохранение списка релизов
        public void SaveReleasesInfo(List<GitHubRelease> cache)
        {
            try
            {
                Directory.CreateDirectory(AppDataPath); // Создаст папку, если её нет

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                string json = JsonConvert.SerializeObject(cache, settings);
                File.WriteAllText(ReleasesFilePath, json);
            }
            catch (Exception ex)
            {
                // Обработка ошибок записи
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        // Загрузка списка релизов
        public List<GitHubRelease> LoadReleasesInfo()
        {
            try
            {
                if (!File.Exists(ReleasesFilePath))
                    return new List<GitHubRelease>();

                string json = File.ReadAllText(ReleasesFilePath);
                return JsonConvert.DeserializeObject<List<GitHubRelease>>(json)
                       ?? new List<GitHubRelease>();
            }
            catch (Exception ex)
            {
                // Обработка ошибок чтения
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                return new List<GitHubRelease>();
            }
        }
    }
}