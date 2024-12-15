using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Shapes;
using LiveChartsCore.SkiaSharpView.Painting;
using ModbusWPF.ViewModel;

namespace ModbusWPF.Views
{
    /// <summary>
    /// HisTrend.xaml 的交互逻辑
    /// </summary>
    public partial class HisTrendWindow : Window
    {
        private DateTime minDateTime;
        private DateTime maxDateTime;
        private Dictionary<string, SKColor> lineColorsDictionary;
        private List<SKColor> lineColors = new List<SKColor>
        {SKColors.Blue, SKColors.Black, SKColors.Yellow, SKColors.Green, SKColors.Red, SKColors.Orange, SKColors.Cyan};

        private readonly DataPointViewModel dataPointViewModel;
        public Dictionary<string, List<float>> slicedRecordDictionary;
        private List<string> dateLabels = new List<string> ();
        private List<string> timeLabels = new List<string>();
        public ObservableCollection<ISeries> ChartSeries = new ObservableCollection<ISeries>();

        public HisTrendWindow(DataPointViewModel dataPointViewModel)
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            this.dataPointViewModel = dataPointViewModel;
            slicedRecordDictionary=new Dictionary<string, List<float>>();

            InitializeData();
            CreateCheckboxes();
            UpdateDateTimeControl();
            LoadAllData();
            RefreshChartSeries();
            cartesianChart.Series = ChartSeries;
        }
        /// <summary>
        /// 初始化数据, 为每个数据点创建一个空的列表，初始化线的颜色
        /// </summary>
        private void InitializeData()
        {
            foreach (var name in dataPointViewModel.DataPointsDictionary.Keys)
            {
                if (!slicedRecordDictionary.ContainsKey(name))
                {
                    slicedRecordDictionary[name] = new List<float>();
                }
            }
            lineColorsDictionary = new Dictionary<string, SKColor>();
            for (int i = 0; i < dataPointViewModel.DataPointsDictionary.Count; i++)
            {
                lineColorsDictionary[dataPointViewModel.DataPointsDictionary.Keys.ToList()[i]] = lineColors[i % 7];
            }
        }

        /// <summary>
        /// 把所有数据复制到slicedRecordDictionary和dateLabels，timeLabels中
        /// </summary>
        private void LoadAllData()
        {
            slicedRecordDictionary.Clear();
            foreach (var key in dataPointViewModel.DataRecordsDictionary.Keys)
            {
                slicedRecordDictionary[key] = new List<float>(dataPointViewModel.DataRecordsDictionary[key]);
            }
            dateLabels.Clear();
            timeLabels.Clear();
            foreach (var date in dataPointViewModel.DateTimeList)
            {
                dateLabels.Add(date.ToString("yyyy-MM-dd"));
                timeLabels.Add(date.ToString("HH:mm:ss"));
            }
        }

        private void CreateCheckboxes()
        {
            var fontSize = (double)FindResource("GlobalFontSize");
            foreach (var name in dataPointViewModel.DataPointsDictionary.Keys)
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

        private void SliceData()
        {
            // 清空slicedRecordDictionary数据
            foreach (var valueList in slicedRecordDictionary.Values)
            {
                valueList.Clear();
            }

            //根据时间范围选择起止index
            int startIndex = FindStartIndex(dataPointViewModel.DateTimeList, minDateTime);
            int endIndex = FindEndIndex(dataPointViewModel.DateTimeList, maxDateTime);
            
            //截取数据放入slicedRecordDictionary
            foreach (var key in dataPointViewModel.DataRecordsDictionary.Keys)
            {
                var fullList = dataPointViewModel.DataRecordsDictionary[key];
                var filteredList = fullList.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
                slicedRecordDictionary[key] = filteredList;
            }

            //截取日期时间数据并转换为字符用作X轴标签
            timeLabels.Clear();
            dateLabels.Clear();
            for (int i = startIndex; i <= endIndex; i++)
            {
                timeLabels.Add(dataPointViewModel.DateTimeList[i].ToString("HH:mm:ss"));
                dateLabels.Add(dataPointViewModel.DateTimeList[i].ToString("yyyy-MM-dd"));
            }

            int FindStartIndex(List<DateTime> dateTimeList, DateTime minDateTime)
            {
                int left = 0, right = dateTimeList.Count - 1, startIndex = -1;

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

            int FindEndIndex(List<DateTime> dateTimeList, DateTime maxDateTime)
            {
                int left = 0, right = dateTimeList.Count - 1, endIndex = -1;

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


        private void RefreshChartSeries()
        {
            ChartSeries.Clear();
            cartesianChart.XAxes = new List<Axis>
            {
                new Axis { Labels = timeLabels },
                new Axis { Labels = dateLabels }
            };

            foreach (CheckBox checkBox in DataSelecter.Items)
            {
                if (checkBox.IsChecked == true)
                {
                    AddChartSeries(checkBox.Content.ToString());
                }
            }
        }


        private void AddChartSeries(string dataPointName)
        {
            var lineSeries = new LineSeries<float>
            {
                Name = dataPointName,
                Values = slicedRecordDictionary[dataPointName],
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                Stroke = new SolidColorPaint(lineColorsDictionary[dataPointName]) { StrokeThickness = 1 },
                LineSmoothness = 0
            };

            ChartSeries.Add(lineSeries);
        }


        private void UpdateDateTimeControl()
        {
            minDateTime = dataPointViewModel.DateTimeList[0];
            maxDateTime =dataPointViewModel.DateTimeList.Last();

            StartDate.SelectedDate = minDateTime.Date;
            StartHour.Text = minDateTime.Hour.ToString("D2");
            StartMinute.Text = minDateTime.Minute.ToString("D2");
            StartSecond.Text = minDateTime.Second.ToString("D2");

            EndDate.SelectedDate = maxDateTime.Date;
            EndHour.Text = maxDateTime.Hour.ToString("D2");
            EndMinute.Text = maxDateTime.Minute.ToString("D2");
            EndSecond.Text = maxDateTime.Second.ToString("D2");
        }

        private void QueryBtnClicked(object sender, EventArgs e)
        {
            if (ValidateAndParseDateTime())
            {
                SliceData();
                RefreshChartSeries();
            }
        }

        private void RefreshBtnClicked(object sender, RoutedEventArgs e)
        {
            LoadAllData();
            RefreshChartSeries();
            UpdateDateTimeControl();
        }

        private bool ValidateAndParseDateTime()
        {
            string minDateString = StartDate.SelectedDate.ToString();
            string minHourString = StartHour.Text;
            string minMinuteString = StartMinute.Text;
            string minSecondString = StartSecond.Text;

            string maxDateString = EndDate.SelectedDate.ToString();
            string maxHourString = EndHour.Text;
            string maxMinuteString = EndMinute.Text;
            string maxSecondString = EndSecond.Text;

            if (!ValidateTime(minHourString, minMinuteString, minSecondString)) return false;
            if (!ValidateTime(maxHourString, maxMinuteString, maxSecondString)) return false;

            minDateTime = ParseDateTime(minDateString, $"{minHourString}:{minMinuteString}:{minSecondString}");
            maxDateTime = ParseDateTime(maxDateString, $"{maxHourString}:{maxMinuteString}:{maxSecondString}");

            if (minDateTime > maxDateTime)
            {
                MessageBox.Show("开始时间不能晚于结束时间", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private DateTime ParseDateTime(string dateString, string timeString)
        {
            var timeParts = timeString.Split(':');
            string hour = timeParts[0], minute = timeParts[1], second = timeParts[2].Split('.')[0];

            DateTime date = DateTime.Parse(dateString);
            return new DateTime(date.Year, date.Month, date.Day, int.Parse(hour), int.Parse(minute), int.Parse(second));
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
