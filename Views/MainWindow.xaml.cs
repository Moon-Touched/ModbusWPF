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
        public string HisCSVPath;
        public MainWindow()
        {
            InitializeComponent();
            string basePath = "C:/ModbusWPF data";
            string dataCSVPath = Path.Combine(basePath,  "data_points.csv");
            string portCSVPath = Path.Combine(basePath,  "port_info.csv");
            dataPointViewModel = new DataPointViewModel(dataCSVPath, portCSVPath);
            DataContext = dataPointViewModel;

            HisCSVPath = Path.Combine("C:/ModbusWPF data", $"DataRecord_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            // 在窗口加载完成后启动任务
            Loaded += (sender, args) =>
            {
                // 使用Task.Run异步执行任务队列处理
                Task.Run(() => dataPointViewModel.ProcessTaskQueue(100));
                Task.Run(() => dataPointViewModel.RecordData(HisCSVPath, 1000));
            };
        }
        
        private void HisBtnClicked(object sender, RoutedEventArgs e)
        {
            var hisTrendWindow = new HisTrendWindow(HisCSVPath,dataPointViewModel.DataPointsDictionary.Keys.ToList());
            hisTrendWindow.Show();
        }
       

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, null);
            Keyboard.ClearFocus();
        }
    }
}