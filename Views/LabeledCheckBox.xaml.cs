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
    /// LabeledCheckBox.xaml 的交互逻辑
    /// </summary>
    public partial class LabeledCheckBox : UserControl
    {
        public LabeledCheckBox()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                "LabelText",
                typeof(string),
                typeof(LabeledCheckBox),
                new PropertyMetadata(""));

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }
        public static readonly DependencyProperty State =
            DependencyProperty.Register(
                "State",
                typeof(bool),
                typeof(LabeledCheckBox),
                new PropertyMetadata(false));
    }
}
