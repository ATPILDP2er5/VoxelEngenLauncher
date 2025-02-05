using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VoxelEngenLauncherRepack.Layouts
{
    /// <summary>
    /// Логика взаимодействия для BrowserTab.xaml
    /// </summary>
    public partial class BrowserTab : UserControl
    {
        public BrowserTab()
        {
            InitializeComponent();
            string htmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Scripts", "index.html");
            // Преобразуем путь в file:// для браузера
            string fileUrl = new Uri(htmlPath).AbsoluteUri;

            // Загружаем страницу в браузер
            CrmBrowse.Load(fileUrl);
        }
    }
}
