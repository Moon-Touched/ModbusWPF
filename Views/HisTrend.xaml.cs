using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ModbusWPF.Views
{
    /// <summary>
    /// HisTrend.xaml 的交互逻辑
    /// </summary>
    public partial class HisTrendWindow : Window
    {
        public HisTrendWindow(string hisCSVPath,List<string> dataPointNameList)
        {
            InitializeComponent();
            var lines = File.ReadAllLines(hisCSVPath).Skip(1); // 跳过表头

            foreach (var line in lines)
            {
                var data = line.Split(',');
            }
        }
    }
}
