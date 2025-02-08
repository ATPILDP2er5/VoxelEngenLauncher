using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VoxelEngenLauncherRepack.Resource.Scripts;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VoxelEngenLauncherRepack.Layouts
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _backgroundTask;

        public Window1()
        {
            InitializeComponent();
            neP_LoadingBar.Minimum = 0;

            // Запускаем фоновую задачу
            _backgroundTask = Task.Run(() => RunBackgroundOperations(_cts.Token));

            // Подписываемся на событие закрытия окна
            this.Closed += async (s, e) =>
            {
                // Ждем завершения задачи или отменяем по таймауту
                await Task.WhenAny(_backgroundTask, Task.Delay(5000));
                _cts.Cancel();
            };
        }

        private void RunBackgroundOperations(CancellationToken token)
        {
            try
            {
                // Обновление UI через Dispatcher
                Dispatcher.Invoke(() =>
                {
                    LoadingText.Text = Application.Current.TryFindResource("FindWindVersion") as string;
                });

                // Запускаем операции с передачей токена отмены
                NetworkManagerGH.GetReleasesAsync(neP_LoadingBar, token).Wait();
                StorageManager.LoadLocalForksAsync(neP_LoadingBar, token).Wait();
            }
            finally
            {
                // Закрываем окно после завершения операций
                Dispatcher.Invoke(() => this.Close());
            }
        }
    }
}
