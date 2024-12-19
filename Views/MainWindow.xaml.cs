using ModbusWPF.ViewModel;
using System.Windows;
using System.Windows.Input;

namespace ModbusWPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel mainViewModel;
        private Window realTrendWindow;
        private Window hisTrendWindow;
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            string basePath = "C:/ModbusWPF data";
            mainViewModel = new MainViewModel(basePath);

            DataContext = mainViewModel;

            Loaded += OnWindowLoaded;


        }

        private void HisBtnClicked(object sender, RoutedEventArgs e)
        {
            HisBtn.IsEnabled = false;
            hisTrendWindow = new HisTrendWindow(mainViewModel.hisCSVPath, mainViewModel.dataCSVPath, mainViewModel.recordLock);
            hisTrendWindow.Show();
            hisTrendWindow.Closed += OnHisWindowClosed;
        }

        private void OnHisWindowClosed(object sender, EventArgs e)
        {
            HisBtn.IsEnabled = true;
        }

        private void RealBtnClicked(object sender, RoutedEventArgs e)
        {
            RealBtn.IsEnabled = false;
            mainViewModel.RealTimePlotViewModel = new RealTimePlotViewModel();
            mainViewModel.InitializeLineSreies();
            realTrendWindow = new RealTrend(mainViewModel.RealTimePlotViewModel);
            realTrendWindow.Show();
            realTrendWindow.Closed += OnRealWindowClosed;
        }

        private void OnRealWindowClosed(object sender, EventArgs e)
        {
            RealBtn.IsEnabled = true;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs args)
        {
            Task.Run(() => mainViewModel.DataPointViewModel.StartTasks(100));
            Task.Run(() => mainViewModel.RecordData(10));
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, null);
            Keyboard.ClearFocus();
        }

    }
}