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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ModbusWPF.Views
{
    /// <summary>
    /// LabeledNumericUpDown.xaml 的交互逻辑
    /// </summary>
    public partial class LabeledNumericUpDown : UserControl
    {
        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                "LabelText", 
                typeof(string), 
                typeof(LabeledNumericUpDown), 
                new PropertyMetadata(""));

        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register(
                "ValueText", 
                typeof(string), 
                typeof(LabeledNumericUpDown),
                new PropertyMetadata(""));

        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public string ValueText
        {
            get { return (string)GetValue(ValueTextProperty); }
            set { SetValue(ValueTextProperty, value); }
        }

        public LabeledNumericUpDown()
        {
            InitializeComponent();
        }
    }
}
