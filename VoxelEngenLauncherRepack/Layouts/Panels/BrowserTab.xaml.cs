using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.Wpf;

namespace VoxelEngenLauncherRepack.Layouts
{
    /// <summary>
    /// Логика взаимодействия для BrowserTab.xaml
    /// </summary>
    public partial class BrowserTab : System.Windows.Controls.UserControl
    {
        public BrowserTab()
        {
            string cachePath = AppDomain.CurrentDomain.BaseDirectory + "Resource\\Data\\Chache\\";
            string logsPath = AppDomain.CurrentDomain.BaseDirectory + "Resource\\User\\Logs\\";
            string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Scripts", "index.html");

            if (!File.Exists(htmlPath) || !File.Exists(logsPath) || !File.Exists(cachePath))
            {
                System.Windows.MessageBox.Show("HTML file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Directory.CreateDirectory(logsPath);
                Directory.CreateDirectory(cachePath);
            }
            var settings = new CefSettings
            {
                CachePath = cachePath,
                LogFile = logsPath
            };
            Cef.Initialize(settings);
            InitializeComponent();
            string fileUrl = new Uri(htmlPath).AbsoluteUri;
            CrmBrowse.DownloadHandler = new CDH();
            CrmBrowse.RequestHandler = new BOL();
            CrmBrowse.Load(fileUrl);
        }

    }
    public class CDH : IDownloadHandler
    {
        public static string AcseptSite = "https://voxelworld.ru/";
        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return url.StartsWith(AcseptSite);
        }

        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            if (callback.IsDisposed) return false;

            string downloadPath = GetDownloadFolder(downloadItem.Url);

            try
            {
                if (!Directory.Exists(downloadPath))
                    Directory.CreateDirectory(downloadPath);

                string fullPath = System.IO.Path.Combine(downloadPath, downloadItem.SuggestedFileName);

                if (File.Exists(fullPath))
                {
                    System.Windows.MessageBox.Show("File already exists!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                callback.Continue(fullPath, showDialog: true);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error preparing download: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            if (downloadItem.IsComplete)
            {
                System.Windows.MessageBox.Show($"File {downloadItem.SuggestedFileName} downloaded successfully!", "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (downloadItem.IsCancelled)
            {
                System.Windows.MessageBox.Show($"Download of {downloadItem.SuggestedFileName} failed!", "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetDownloadFolder(string url)
        {
            if (url.Contains(AcseptSite))
            {
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Data", "Mods");
            }

            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", "Other");
        }
    }
    public class BOL : IRequestHandler
    {
        public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            return false;
        }

        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            return null;
        }

        public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            // Если URL начинается с tg://, открываем его через системный вызов и отменяем навигацию в CefSharp
            if (request.Url.StartsWith("tg://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(request.Url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка при открытии ссылки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return true; // Отменяем дальнейшую обработку этого URL в CefSharp
            }
            return false; // Для остальных URL продолжаем нормальную обработку
        }

        public bool OnCertificateError(IWebBrowser chromiumWebBrowser, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
        {
            return false;
        }

        public void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }

        public bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            return false;
        }

        public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status, int errorCode, string errorMessage)
        {
        }

        public void OnRenderViewReady(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
        }

        public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
        {
            return false;
        }
    }
}
