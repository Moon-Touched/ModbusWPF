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
using LiveCharts;
using LiveCharts.Wpf;
using ModbusWPF.Models;
using System.Globalization;
using LiveCharts.Definitions.Charts;
using System.Diagnostics;

namespace ModbusWPF.Views
{
    /// <summary>
    /// HisTrend.xaml 的交互逻辑
    /// </summary>
    public partial class HisTrendWindow : Window
    {
        private DateTime minDateTime;
        private DateTime maxDateTime;
        private string filePath;
        private List<string> dataPointNames;
        private Dictionary<string, List<string>> dataRecordDictionary;
        private ListView dataSelecter;
        public SeriesCollection chartSeriesCollection { get; set; }
        public Func<double, string> YAxisFormatter { get; set; }
        public Func<double, string> XAxisFormatter { get; set; }

        public HisTrendWindow(string hisCSVPath)
        {
            InitializeComponent();
            YAxisFormatter = value => value.ToString("F2", CultureInfo.InvariantCulture);
            XAxisFormatter = value => DateTime.FromOADate(value).ToString("yyyy-MM-dd HH:mm:ss");

            filePath = hisCSVPath;
            var lines = File.ReadAllLines(filePath);
            dataPointNames = lines[0].Split(',').Skip(2).ToList();//跳过日期时间
            dataSelecter = FindName("DataSelecter") as ListView;
            var fontSize = (double)FindResource("GlobalFontSize");
            foreach (var name in dataPointNames)
            {
                var checkbox = new CheckBox();
                checkbox.Name = name;
                checkbox.FontSize = fontSize;
                checkbox.Content = name;
                checkbox.IsChecked = true;
                checkbox.Checked += (s, e) => refreshChartSeriesCollection();
                checkbox.Unchecked += (s, e) => refreshChartSeriesCollection();
                dataSelecter.Items.Add(checkbox);
            }

            GetMinMaxDateTime(lines[2], lines.Last());
            updateDateTimeControl();

            chartSeriesCollection = new SeriesCollection();
            refreshDataAndSeries();
            DataContext = this;
        }


        private void refreshDataAndSeries()
        {
            dataRecordDictionary = new Dictionary<string, List<string>>();
            var lines = File.ReadAllLines(filePath).Skip(1); // 跳过表头
            foreach (var line in lines)
            {
                List<string> data = line.Split(',').ToList();
                AddDataToDictionary("date", data[0]);
                AddDataToDictionary("time", data[1]);

                for (int i = 0; i < dataPointNames.Count; i++)
                {
                    string dataPointName = dataPointNames[i];
                    AddDataToDictionary(dataPointName, data[i + 2]);//跳过日期时间
                }
            }
            refreshChartSeriesCollection();
            return;
        }

        private void refreshChartSeriesCollection()
        {
            cartesianChart.AxisX[0].LabelFormatter = value =>
            {
                return DateTime.FromOADate(value).ToString("yyyy-MM-dd HH:mm:ss");
            };
            chartSeriesCollection.Clear();

            var dateList = dataRecordDictionary["date"];
            var timeList = dataRecordDictionary["time"];
            List<DateTime> dateTimeData=new List<DateTime>();
            for (int i = 0; i < dateList.Count; i++)
            {
                string dateString = dateList[i];
                string timeString = timeList[i];
                DateTime dateTime = ParseDateTime(dateString, timeString);
                dateTimeData.Add(dateTime);
            }
            

            var timeValues = new ChartValues<double>(dateTimeData.ConvertAll(x => x.ToOADate()));
            foreach (var value in timeValues)
            {
                Debug.WriteLine($"OADate: {value}, DateTime: {DateTime.FromOADate(value)}");
            }
            //cartesianChart.AxisX[0].MinValue = minDateTime.ToOADate();
            //cartesianChart.AxisX[0].MaxValue = maxDateTime.ToOADate();

            foreach (var item in dataSelecter.Items)
            {
                var checkBox = item as CheckBox;
                string dataPointName = checkBox.Content.ToString();
                if (!checkBox.IsChecked.GetValueOrDefault())  // 如果未选中，跳过
                {
                    continue;
                }

                var lineSeries = new LineSeries
                {
                    Title = dataPointName,
                    Values = new ChartValues<float>(),
                    PointGeometry = null,
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0
                };

                var dataList = dataRecordDictionary[dataPointName];
                List<float> convertedValues = ConvertDataList(dataList);
                lineSeries.Values = new ChartValues<float>(convertedValues);
                chartSeriesCollection.Add(lineSeries);
            }
            cartesianChart.Update(true, true);
        }

        private List<float> ConvertDataList(List<string> dataList)
        {
            var convertedValues = new List<float>();

            foreach (var data in dataList)
            {
                float value = ConvertStringToFloat(data);
                if (!float.IsNaN(value)) // 只添加有效的 float 值
                {
                    convertedValues.Add(value);
                }
            }

            return convertedValues;
        }

        private float ConvertStringToFloat(string data)
        {
            if (float.TryParse(data, out float value))
            {
                return value;
            }
            else if (data.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return 1f;
            }
            else if (data.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return 0f;
            }
            else
            {
                return float.NaN; // 无效数据返回 NaN
            }
        }

        private void GetMinMaxDateTime(string firstLine, string lastLine)
        {
            string[] firstData = firstLine.Split(',');
            string firstDateString = firstData[0];
            string firstTimeString = firstData[1];
            minDateTime = ParseDateTime(firstDateString, firstTimeString);

            string[] lastData = lastLine.Split(',');
            string lastDateString = lastData[0];
            string lastTimeString = lastData[1];

            maxDateTime = ParseDateTime(lastDateString, lastTimeString);
            return;
        }

        private void updateDateTimeControl()
        {
            StartDate.SelectedDate = minDateTime.Date;
            StartHour.Text = minDateTime.Hour.ToString("D2");
            StartMinute.Text = minDateTime.Minute.ToString("D2");
            StartSecond.Text = minDateTime.Second.ToString("D2");

            EndDate.SelectedDate = maxDateTime.Date;
            EndHour.Text = maxDateTime.Hour.ToString("D2");
            EndMinute.Text = maxDateTime.Minute.ToString("D2");
            EndSecond.Text = maxDateTime.Second.ToString("D2");
            return;
        }

        private DateTime ParseDateTime(string dateString, string timeString)
        {
            // 解析日期和时间字符串，返回 DateTime 类型
            string[] timeParts = timeString.Split(':');
            string hour = timeParts[0];
            string minute = timeParts[1];
            string second = timeParts[2].Split('.')[0]; // 去除毫秒

            DateTime date = DateTime.Parse(dateString);
            return new DateTime(date.Year, date.Month, date.Day, int.Parse(hour), int.Parse(minute), int.Parse(second));
        }

        private void AddDataToDictionary(string key, string value)
        {
            // 如果字典中没有这个键，则初始化一个新的 List<string>
            if (!dataRecordDictionary.ContainsKey(key))
            {
                dataRecordDictionary[key] = new List<string>();
            }

            // 向字典中添加数据
            dataRecordDictionary[key].Add(value);
        }

        private void QueryBtnClicked(object sender, EventArgs e)
        {

        }

        private void refreshBtnClicked(object sender, RoutedEventArgs e)
        {
            refreshDataAndSeries();
        }
    }
}
