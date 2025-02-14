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
            InitializeWebView2Async();
        }

        private async void InitializeWebView2Async()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string cachePath = Path.Combine(localAppData, "VEL", "Resource", "Data", "WebView2Cache");
            _htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Scripts", "index.html");
            string logsPath = Path.Combine(localAppData, "VEL", "Resource", "Data", "Logs");

            // Проверка и создание директорий
            if (!File.Exists(_htmlPath) || !Directory.Exists(localAppData) || !Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(localAppData);
                Directory.CreateDirectory(cachePath);
                MessageBox.Show("HTML file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                var environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: cachePath,
                    options: new CoreWebView2EnvironmentOptions()
                    {
                        AdditionalBrowserArguments = $"--log-file={Path.Combine(logsPath, "webview2.log")}"
                    });

                await WebView2Control.EnsureCoreWebView2Async(environment);

                // Навигация
                WebView2Control.Source = new Uri(_htmlPath);
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
                ? Path.Combine(appData,"VLE", "Resource", "Data", "Mods")
                : Path.Combine(appData, "VLE", "Downloads", "Other");
        }
    }
}