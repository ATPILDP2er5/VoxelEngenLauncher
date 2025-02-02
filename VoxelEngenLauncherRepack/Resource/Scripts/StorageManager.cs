using System.Collections.Concurrent;
using System.IO;
using System.Windows.Controls;
using Newtonsoft.Json;

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
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource/Data/Forks");
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

        // 3. Публичное свойство с потокобезопасным доступом
        public static List<Forks> LocalForks => _localForks.ToList();
    }
}