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
    /// LabeledDecimalUpDown.xaml 的交互逻辑
    /// </summary>
    public partial class LabeledDecimalUpDown : UserControl
    {
        public LabeledDecimalUpDown()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
                "LabelText",
                typeof(string),
                typeof(LabeledDecimalUpDown),
                new PropertyMetadata("")
                );

        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register(
                "ValueText",
                typeof(string),
                typeof(LabeledDecimalUpDown),
                new PropertyMetadata("")
                );


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

        public float MinValue { get; set; } = float.MinValue;
        public float MaxValue { get; set; } = float.MaxValue;

        private void Add_Btn_Clicked(object sender, RoutedEventArgs e)
        {
            float value = float.Parse(ValueText);
            value += 0.1f;
            if (value <= MaxValue)
            {
                //value = (float)Math.Round(value, 2);
                ValueText = value.ToString("F2");
            }
        }

        private void Sub_Btn_Clicked(object sender, RoutedEventArgs e)
        {
            float value = float.Parse(ValueText);
            value -= 0.1f;
            if (value >= MinValue)
            {
                //value = (float)Math.Round(value, 2);
                ValueText = value.ToString("F2");
            }
        }
    }
}
