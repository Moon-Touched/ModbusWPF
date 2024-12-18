using System.Diagnostics;
using System.Text;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using ModbusWPF.Models;
using ModbusWPF.ViewModel;

namespace ModbusWPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataPointViewModel dataPointViewModel;
        public string hisCSVPath;
        public string hisBinaryPath;
        public string dataCSVPath;
        public string portCSVPath;
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            string basePath = "C:/ModbusWPF data";
            dataCSVPath = Path.Combine(basePath, "data_points.csv");
            portCSVPath = Path.Combine(basePath, "port_info.csv");
            hisCSVPath = Path.Combine(basePath, $"DataRecord_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            hisBinaryPath= Path.Combine(basePath, $"DataRecord_{DateTime.Now:yyyyMMdd_HHmmss}.bin");

            dataPointViewModel = new DataPointViewModel(dataCSVPath, portCSVPath);
            DataContext = dataPointViewModel;

            // 在窗口加载完成后启动任务
            Loaded += OnWindowLoaded;
        }

        private void HisBtnClicked(object sender, RoutedEventArgs e)
        {
            HisBtn.IsEnabled = false;
            var hisTrendWindow = new HisTrendWindow(hisBinaryPath, dataCSVPath, dataPointViewModel);
            hisTrendWindow.Show();
            hisTrendWindow.Closed += OnHisWindowClosed;
        }

        private void OnHisWindowClosed(object sender, EventArgs e)
        {
            HisBtn.IsEnabled = true;
        }
        private void OnWindowLoaded(object sender, RoutedEventArgs args)
        {
            Task.Run(() => dataPointViewModel.StartTasks(100));
            Task.Run(() => dataPointViewModel.RecordData(hisCSVPath, hisBinaryPath, 10));
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, null);
            Keyboard.ClearFocus();
        }

    }
}