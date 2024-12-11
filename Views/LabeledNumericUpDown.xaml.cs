using MaterialDesignThemes.Wpf;
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
                new PropertyMetadata("")
                );

        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register(
                "ValueText", 
                typeof(int), 
                typeof(LabeledNumericUpDown),
                new PropertyMetadata(0)
                );

        public NumericUpDown NumericUpDownControl
        {
            get { return this.FindName("NumericUpDown") as NumericUpDown; }
        }

        public int Maximum
        {
            get { return NumericUpDownControl.Maximum; }
            set
            {
                if (NumericUpDown != null)
                {
                    NumericUpDown.Maximum = value;
                }
            }
        }

        public int Minimum
        {
            get { return NumericUpDownControl.Minimum; }
            set
            {
                if (NumericUpDown != null)
                {
                    NumericUpDown.Minimum = value;
                }
            }
        }

        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public int ValueText
        {
            get { return (int)GetValue(ValueTextProperty); }
            set { SetValue(ValueTextProperty, value); }
        }

        public LabeledNumericUpDown()
        {
            InitializeComponent();
        }
    }
}
