using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;

namespace VoxelEngenLauncherRepack.Layouts
{
    public partial class BrowserTab : UserControl
    {
        private static readonly string AcceptSite = "https://voxelworld.ru/";
        private string _htmlPath;

        public BrowserTab()
        {
            InitializeComponent();
            _htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Scripts", "index.html");
            if (!File.Exists(_htmlPath))
            {
                MessageBox.Show("HTML file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            InitializeWebView2Async(_htmlPath, WebView2Control);
        }

        public static async void InitializeWebView2Async(string addres, object trigger, string? EndPath = "VEL\\Download")
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string cachePath = Path.Combine(localAppData, "VEL", "Resource", "Data", "WebView2Cache");
            string logsPath = Path.Combine(localAppData, "VEL", "Resource", "Data", "Logs");
            string downloadPath = Path.Combine(localAppData, EndPath); // Папка для загрузок

            // Проверка и создание директорий
            Directory.CreateDirectory(cachePath);
            Directory.CreateDirectory(logsPath);
            Directory.CreateDirectory(downloadPath);

            try
            {
                var environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: cachePath,
                    options: new CoreWebView2EnvironmentOptions()
                    {
                        AdditionalBrowserArguments = $"--log-file={Path.Combine(logsPath, "webview2.log")}"
                    });

                var webView = trigger as WebView2;
                await webView.EnsureCoreWebView2Async(environment);

                // Установка пути загрузки файлов
                webView.CoreWebView2.DownloadStarting += (sender, args) =>
                {
                    string fileName = Path.GetFileName(args.ResultFilePath);
                    string newFilePath = Path.Combine(downloadPath, fileName);
                    args.ResultFilePath = newFilePath; // Подтверждаем, что путь обработан вручную
                };

                // Навигация
                webView.Source = new Uri(addres);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Обработка tg:// ссылок
            if (e.Uri.StartsWith("tg://", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                try
                {
                    Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии ссылки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            string downloadUrl = e.DownloadOperation.Uri;
            if (!downloadUrl.StartsWith(AcceptSite))
            {
                e.Cancel = true;
                return;
            }

            string downloadPath = GetDownloadFolder(downloadUrl);
            string fullPath = Path.Combine(downloadPath, e.ResultFilePath);

            if (File.Exists(fullPath))
            {
                MessageBox.Show("File already exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            e.ResultFilePath = fullPath;
            e.Handled = true;

            e.DownloadOperation.StateChanged += (s, args) =>
            {
                if (e.DownloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    MessageBox.Show($"{Application.Current.TryFindResource("DownloadSucA")} {e.ResultFilePath} {Application.Current.TryFindResource("DownloadSucC")}",
                                  $"{Application.Current.TryFindResource("Succes")}",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
        }

        private string GetDownloadFolder(string url)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return url.Contains(AcceptSite)
                ? Path.Combine(appData, "VLE", "Resource", "Data", "Mods")
                : Path.Combine(appData, "VLE", "Downloads", "Other");
        }
    }
}