using ModbusWPF.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ModbusWPF.Views
{
    /// <summary>
    /// RealTrend.xaml 的交互逻辑
    /// </summary>
    public partial class RealTrend : Window
    {
        private RealTimePlotViewModel RealTimePlotViewModel { get; set; }
        public RealTrend(RealTimePlotViewModel realTimePlotViewModel)
        {
            InitializeComponent();
            RealTimePlotViewModel = realTimePlotViewModel;
            DataContext = RealTimePlotViewModel;
            CreateCheckboxes();
        }
        private void CreateCheckboxes()
        {
            var fontSize = (double)FindResource("GlobalFontSize");
            foreach (var name in RealTimePlotViewModel.LineSeriesDictionary.Keys)
            {
                var checkbox = new CheckBox
                {
                    Name = name,
                    FontSize = fontSize,
                    Content = name,
                    IsChecked = true
                };
                Binding binding = new Binding
                {
                    Source = RealTimePlotViewModel.LineSeriesDictionary,
                    Path = new PropertyPath($"[{name}].IsVisible"),
                    Mode = BindingMode.TwoWay
                };
                checkbox.SetBinding(CheckBox.IsCheckedProperty, binding);
                DataSelecter.Items.Add(checkbox);
            }
        }
    }
}
