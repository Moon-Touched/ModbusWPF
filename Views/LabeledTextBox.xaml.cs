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
    /// LabeledTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class LabeledTextBox : UserControl
    {
        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                "LabelText", 
                typeof(string), 
                typeof(LabeledTextBox), 
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register(
                "ValueText", 
                typeof(string), 
                typeof(LabeledTextBox), 
                new PropertyMetadata(string.Empty));

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public string ValueText
        {
            get => (string)GetValue(ValueTextProperty);
            set => SetValue(ValueTextProperty, value);
        }

        public LabeledTextBox()
        {
            InitializeComponent();
        }
    }
}
