using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        public MainWindow()
        {
            InitializeComponent();
            dataPointViewModel = new DataPointViewModel();
            DataContext = dataPointViewModel;

            // 在窗口加载完成后启动任务
            Loaded += (sender, args) =>
            {
                // 使用Task.Run异步执行任务队列处理
               Task.Run(() => dataPointViewModel.ProcessTaskQueue());
            };
        }
        

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, null);
            Keyboard.ClearFocus();
        }
    }
}