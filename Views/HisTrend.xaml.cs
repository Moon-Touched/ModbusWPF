using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ModbusWPF.ViewModel;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Utilities;
using System.Xml.Linq;

namespace ModbusWPF.Views
{
    /// <summary>
    /// HisTrend.xaml 的交互逻辑
    /// </summary>
    public partial class HisTrendWindow : Window
    {
        private double minDateTime;
        private double maxDateTime;

        private DateTime start;
        private DateTime end;

        private Dictionary<string, OxyColor> lineColorsDictionary;
        private List<OxyColor> lineColors = new List<OxyColor>
        {OxyColors.Blue, OxyColors.Black, OxyColors.Yellow, OxyColors.Green, OxyColors.Red, OxyColors.Orange, OxyColors.Cyan};
        /// <summary>
        /// 存储和查找变量在第几列
        /// </summary>
        private Dictionary<string, int> dataIndexDictionary;
        private Dictionary<string, string> dataTypeDictionary;
        private string[] fullRecord;
        private double[] dateTimeDoubleList;
        private Dictionary<string, DataPoint[]> slicedDictionary;
        private Dictionary<string, double[]> fullDictionary;
        private string hisCSVPath;
        public readonly object RecordLock;

        private PlotModel plotModel;

        public HisTrendWindow(string hisCSVPath, string dataCSVPath, DataPointViewModel dataPointViewModel)
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            dataIndexDictionary = new Dictionary<string, int>();
            dataTypeDictionary = new Dictionary<string, string>();
            fullDictionary = new Dictionary<string, double[]>();
            slicedDictionary=new Dictionary<string, DataPoint[]>();
            dateTimeDoubleList = [];
            RecordLock = dataPointViewModel.RecordLock;
            this.hisCSVPath = hisCSVPath;
            InitializeData(dataCSVPath, this.hisCSVPath);
            CreateCheckboxes();
            LoadAllData();
            UpdateDateTimeControl();
            SliceData(0, fullRecord.Length);
            RefreshChartSeries();
        }
        /// <summary>
        /// 初始化数据, 读取变量列表和数据类型，分配线条颜色。
        /// </summary>
        private void InitializeData(string dataCSVPath, string hisCSVPath)
        {
            var lines = File.ReadAllLines(dataCSVPath).Skip(1);
            foreach (var line in lines)
            {
                var info = line.Split(',');
                string name = info[0];
                string dataType = info[1];
                dataTypeDictionary[name] = dataType;
            }

            string[] header;
            lock (RecordLock)
            {
                header = File.ReadAllLines(hisCSVPath)[0].Split(",");
            }
            for (int i = 2; i < header.Length; i++)
            {
                string name = header[i];
                dataIndexDictionary[name] = i;
            }

            lineColorsDictionary = new Dictionary<string, OxyColor>();
            for (int i = 0; i < dataTypeDictionary.Keys.Count; i++)
            {
                lineColorsDictionary[dataTypeDictionary.Keys.ToList()[i]] = lineColors[i % lineColors.Count];
            }
        }

        /// <summary>
        /// 加锁读取csv，然后将数据分配到对应的数组中。
        /// </summary>
        private void LoadAllData()
        {
            start = DateTime.Now;
            lock (RecordLock)
            {
                fullRecord = File.ReadAllLines(hisCSVPath).Skip(1).ToArray();
            }
            InfoBlock.Text = $"共有{fullRecord.Length}条数据\n";

            int count = fullRecord.Length;
            InitializeArray(count);
            AddData(count);
        }

        private void AddData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string[] dataStrings = fullRecord[i].Split(",");
                var dateTime = ParseDateTime(dataStrings[0], dataStrings[1]);
                dateTimeDoubleList[i] = dateTime.ToOADate()+1;
                foreach (var name in dataTypeDictionary.Keys)
                {
                    if (dataTypeDictionary[name] == "bool" || dataTypeDictionary[name] == "bool_int")
                    {
                        if (bool.Parse(dataStrings[dataIndexDictionary[name]]))
                        {
                            fullDictionary[name][i] = 1;
                        }
                        else
                        {
                            fullDictionary[name][i] = 0;
                        }
                    }
                    else
                    {
                        fullDictionary[name][i] = double.Parse(dataStrings[dataIndexDictionary[name]]);
                    }
                }
            }
        }

        private void InitializeArray(int count)
        {
            dateTimeDoubleList = new double[count];
            foreach (var name in dataTypeDictionary.Keys)
            {
                fullDictionary[name] = new double[count];
                slicedDictionary[name]= new DataPoint[count];
            }
        }

        /// <summary>
        /// 为每个数据点创建复选框
        /// </summary>
        private void CreateCheckboxes()
        {
            var fontSize = (double)FindResource("GlobalFontSize");
            foreach (var name in dataTypeDictionary.Keys)
            {
                var checkbox = new CheckBox
                {
                    Name = name,
                    FontSize = fontSize,
                    Content = name,
                    IsChecked = true
                };
                checkbox.Checked += CheckboxCheckedChanged;
                checkbox.Unchecked += CheckboxCheckedChanged;
                DataSelecter.Items.Add(checkbox);

            }
        }

        private void CheckboxCheckedChanged(object sender, RoutedEventArgs e)
        {
            RefreshChartSeries();
        }

        private void SliceDataByTimeRange()
        {
            //根据时间范围选择起止index
            int startIndex = FindStartIndex(dateTimeDoubleList, minDateTime);
            int endIndex = FindEndIndex(dateTimeDoubleList, maxDateTime);
            int length = endIndex - startIndex;

            //截取数据放入slicedRecordDictionary
            SliceData(startIndex, length);

            int FindStartIndex(double[] dateTimeList, double minDateTime)
            {
                int left = 0, right = dateTimeList.Length - 1, startIndex = -1;

                while (left <= right)
                {
                    int mid = left + (right - left) / 2;

                    if (dateTimeList[mid] >= minDateTime)
                    {
                        startIndex = mid; // 可能是起始索引，继续向左搜索
                        right = mid - 1;
                    }
                    else
                    {
                        left = mid + 1;
                    }
                }

                return startIndex;
            }

            int FindEndIndex(double[] dateTimeList, double maxDateTime)
            {
                int left = 0, right = dateTimeList.Length - 1, endIndex = -1;

                while (left <= right)
                {
                    int mid = left + (right - left) / 2;

                    if (dateTimeList[mid] <= maxDateTime)
                    {
                        endIndex = mid; // 可能是终止索引，继续向右搜索
                        left = mid + 1;
                    }
                    else
                    {
                        right = mid - 1;
                    }
                }

                return endIndex;
            }
        }

        private void SliceData(int startIndex, int length)
        {
            foreach (var name in dataTypeDictionary.Keys)
            {
                slicedDictionary[name] = new DataPoint[length];
                for (int i = 0; i < length; i++)
                {
                    slicedDictionary[name][i] = new DataPoint(dateTimeDoubleList[i + startIndex], fullDictionary[name][i+startIndex]);
                }
            }

            end = DateTime.Now;
            InfoBlock.Text = $"共有{fullRecord.Length}条数据\n截取了{slicedDictionary.Values.First().Length}\n{(end - start).TotalMilliseconds}ms";
        }

        /// <summary>
        /// 转换时间日期为字符串用作X轴标签并根据选中的复选框刷新图表
        /// </summary>
        private void RefreshChartSeries()
        {
            plotModel = new PlotModel();
            plotModel.Legends.Add(new Legend { LegendPosition=LegendPosition.RightTop, LegendPlacement=LegendPlacement.Inside});
            plotModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd\nHH:mm:ss",
            });

            foreach (CheckBox checkBox in DataSelecter.Items)
            {
                if (checkBox.IsChecked == true)
                {
                    AddChartSeries(checkBox.Content.ToString());
                }
            }
            Plot.Model = plotModel;
        }

        private void AddChartSeries(string dataName)
        {
            plotModel.Series.Add(new LineSeries
            {
                Title = dataName,
                ItemsSource = slicedDictionary[dataName],
                DataFieldX = "DateTime",
                DataFieldY = "Value",
                Color = lineColorsDictionary[dataName]
            });

        }

        /// <summary>
        /// 将日期时间控件更新为全部数据的起止时间
        /// </summary>
        private void UpdateDateTimeControl()
        {
            minDateTime = dateTimeDoubleList[0];
            maxDateTime = dateTimeDoubleList.Last();

            var min= DateTimeAxis.ToDateTime(minDateTime);
            StartDate.SelectedDate = min.Date;
            StartHour.Text = min.Hour.ToString("D2");
            StartMinute.Text = min.Minute.ToString("D2");
            StartSecond.Text = min.Second.ToString("D2");

            var max= DateTimeAxis.ToDateTime(maxDateTime);
            EndDate.SelectedDate = max.Date;
            EndHour.Text = max.Hour.ToString("D2");
            EndMinute.Text = max.Minute.ToString("D2");
            EndSecond.Text = max.Second.ToString("D2");
        }

        private void QueryBtnClicked(object sender, EventArgs e)
        {
            if (ValidateAndParseDateTime())
            {
                SliceDataByTimeRange();
                RefreshChartSeries();
            }
        }

        private void RefreshBtnClicked(object sender, RoutedEventArgs e)
        {
            LoadAllData();
            UpdateDateTimeControl();
            SliceData(0, fullRecord.Length);
            RefreshChartSeries();
        }

        private bool ValidateAndParseDateTime()
        {
            string minDateString = StartDate.SelectedDate.ToString().Split(" ")[0];
            string minHourString = StartHour.Text;
            string minMinuteString = StartMinute.Text;
            string minSecondString = StartSecond.Text;

            string maxDateString = EndDate.SelectedDate.ToString().Split(" ")[0];
            string maxHourString = EndHour.Text;
            string maxMinuteString = EndMinute.Text;
            string maxSecondString = EndSecond.Text;

            if (!ValidateTime(minHourString, minMinuteString, minSecondString)) return false;
            if (!ValidateTime(maxHourString, maxMinuteString, maxSecondString)) return false;

            minDateTime =DateTimeAxis.ToDouble( ParseDateTime(minDateString, $"{minHourString}:{minMinuteString}:{minSecondString}"));
            maxDateTime = DateTimeAxis.ToDouble(ParseDateTime(maxDateString, $"{maxHourString}:{maxMinuteString}:{maxSecondString}"));

            if (minDateTime > maxDateTime)
            {
                MessageBox.Show("开始时间不能晚于结束时间", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private DateTime ParseDateTime(string dateString, string timeString)
        {
            DateOnly date = DateOnly.Parse(dateString);
            TimeOnly time = TimeOnly.Parse(timeString);

            return new DateTime(date, time);
        }

        private bool ValidateTime(string hourString, string minuteString, string secondString)
        {
            if (!int.TryParse(hourString, out int hour) || hour < 0 || hour > 23)
            {
                ShowTimeError("小时数应在 0 到 23 之间");
                return false;
            }
            if (!int.TryParse(minuteString, out int minute) || minute < 0 || minute > 59)
            {
                ShowTimeError("分钟数应在 0 到 59 之间");
                return false;
            }
            if (!int.TryParse(secondString, out int second) || second < 0 || second > 59)
            {
                ShowTimeError("秒数应在 0 到 59 之间");
                return false;
            }
            return true;
        }

        private void ShowTimeError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
