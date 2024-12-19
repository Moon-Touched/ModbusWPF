using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using ModbusWPF.ViewModel;

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
