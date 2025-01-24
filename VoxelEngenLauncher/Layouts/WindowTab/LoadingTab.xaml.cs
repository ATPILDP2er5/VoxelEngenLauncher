using System.Net.Http;
using System.Text.Json;
using System.Windows;

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
            ShareRep();
        }


        static async Task ShareRep()
        {
            string repoOwner = "MihailRis"; // Владелец репозитория
            string repoName = "VoxelEngine-Cpp"; // Название репозитория
            string apiUrl = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases";

            try
            {
                List<GitHubRelease> releases = await GetReleasesAsync(apiUrl);

                Console.WriteLine($"Релизы для репозитория {repoOwner}/{repoName}:");
                foreach (var release in releases)
                {
                    Console.WriteLine($"- Название: {release.Name}");
                    Console.WriteLine($"  Версия: {release.TagName}");
                    Console.WriteLine($"  Дата публикации: {release.PublishedAt}");
                    Console.WriteLine($"  Ссылка: {release.HtmlUrl}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        static async Task<List<GitHubRelease>> GetReleasesAsync(string url)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "C# App"); // GitHub требует User-Agent

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<GitHubRelease>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }

    public class GitHubRelease
    {
        public string Name { get; set; }
        public string TagName { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string HtmlUrl { get; set; }
    }
}


