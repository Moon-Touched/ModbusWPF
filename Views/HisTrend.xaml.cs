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
using ModbusWPF.Models;
using System.Diagnostics;
using System.Data.Common;

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

        private Dictionary<string, int> dataIndexDictionary;
        private Dictionary<string, string> dataTypeDictionary;
        private string[] fullRecord;
        private DateTime[] dateTimeList;
        private string[] slicedDateLabels;
        private string[] fullDateLabels;
        private string[] slicedTimeLabels;
        private string[] fullTimeLabels;
        private Dictionary<string, bool[]> slicedBoolDictionary;
        private Dictionary<string, bool[]> fullBoolDictionary;
        private Dictionary<string, short[]> slicedInt16Dictionary;
        private Dictionary<string, short[]> fullInt16Dictionary;
        private Dictionary<string, float[]> slicedFloatDictionary;
        private Dictionary<string, float[]> fullFloatDictionary;
        string hisCSVPath;
        public readonly object RecordLock;

        public ObservableCollection<ISeries> ChartSeries = new ObservableCollection<ISeries>();

        public HisTrendWindow(string hisCSVPath, string dataCSVPath, DataPointViewModel dataPointViewModel)
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            dataIndexDictionary = new Dictionary<string, int>();
            dataTypeDictionary = new Dictionary<string, string>();
            slicedBoolDictionary = new Dictionary<string, bool[]>();
            fullBoolDictionary = new Dictionary<string, bool[]>();
            slicedInt16Dictionary = new Dictionary<string, short[]>();
            fullInt16Dictionary = new Dictionary<string, short[]>();
            slicedFloatDictionary = new Dictionary<string, float[]>();
            fullFloatDictionary = new Dictionary<string, float[]>();
            dateTimeList = [];
            slicedDateLabels = [];
            fullDateLabels = [];
            slicedTimeLabels = [];
            fullTimeLabels = [];
            RecordLock = dataPointViewModel.RecordLock;
            this.hisCSVPath = hisCSVPath;
            InitializeData(dataCSVPath, this.hisCSVPath);
            CreateCheckboxes();
            LoadAllData();
            UpdateDateTimeControl();
            RefreshChartSeries();
        }
        /// <summary>
        /// 初始化数据, 读取变量列表和数据类型，分配线条颜色。
        /// </summary>
        private void InitializeData(string dataCSVPath,string hisCSVPath)
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

            lineColorsDictionary = new Dictionary<string, SKColor>();
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
            lock (RecordLock)
            {
                fullRecord = File.ReadAllLines(hisCSVPath).Skip(1).ToArray();
            }
            InfoBlock.Text = $"共有{fullRecord.Length}条数据";

            int count = fullRecord.Length;
            InitializeArray(count);
            AddData(count);
        }

        private void AddData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                string[] dataStrings = fullRecord[i].Split(",");
                fullDateLabels[i] = dataStrings[0];
                slicedDateLabels[i] = dataStrings[0];
                fullTimeLabels[i] = dataStrings[1];
                slicedTimeLabels[i] = dataStrings[1];
                dateTimeList[i] = ParseDateTime(dataStrings[0], dataStrings[1]);
                foreach(var name in dataTypeDictionary.Keys)
                {
                    int j=dataIndexDictionary[name];
                    switch (dataTypeDictionary[name])
                    {
                        case "bool":
                            slicedBoolDictionary[name][i] = bool.Parse(dataStrings[j]);
                            fullBoolDictionary[name][i] = bool.Parse(dataStrings[j]);
                            break;
                        case "int16":
                            slicedInt16Dictionary[name][i] = short.Parse(dataStrings[j]);
                            fullInt16Dictionary[name][i] = short.Parse(dataStrings[j]);
                            break;
                        case "float32":
                            slicedFloatDictionary[name][i] = float.Parse(dataStrings[j]);
                            fullFloatDictionary[name][i] = float.Parse(dataStrings[j]);
                            break;
                        case "float_int":
                            slicedFloatDictionary[name][i] = float.Parse(dataStrings[j]);
                            fullFloatDictionary[name][i] = float.Parse(dataStrings[j]);
                            break;
                        case "bool_int":
                            slicedBoolDictionary[name][i] = bool.Parse(dataStrings[j]);
                            fullBoolDictionary[name][i] = bool.Parse(dataStrings[j]);
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported data type: {dataTypeDictionary[name]}");
                    }
                }
            }
        }

        private void InitializeArray(int count)
        {
            dateTimeList = new DateTime[count];
            slicedTimeLabels=new string[count];
            fullTimeLabels = new string[count];
            slicedDateLabels = new string[count];
            fullDateLabels = new string[count];
            foreach (var name in dataTypeDictionary.Keys)
            {
                switch (dataTypeDictionary[name])
                {
                    case "bool":
                        slicedBoolDictionary[name] = new bool[count];
                        fullBoolDictionary[name] = new bool[count];
                        break;
                    case "int16":
                        slicedInt16Dictionary[name] = new short[count];
                        fullInt16Dictionary[name] = new short[count];
                        break;
                    case "float32":
                        slicedFloatDictionary[name] = new float[count];
                        fullFloatDictionary[name] = new float[count];
                        break;
                    case "float_int":
                        slicedFloatDictionary[name] = new float[count];
                        break;
                    case "bool_int":
                        slicedBoolDictionary[name] = new bool[count];
                        fullBoolDictionary[name] = new bool[count];
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported data type: {dataTypeDictionary[name]}");
                }
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
                if (dataTypeDictionary[name]!="bool" && dataTypeDictionary[name] != "bool_int")
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
        }

        private void CheckboxCheckedChanged(object sender, RoutedEventArgs e)
        {
            RefreshChartSeries();
        }

        private void SliceData()
        {
            //根据时间范围选择起止index
            int startIndex = FindStartIndex(dateTimeList, minDateTime);
            int endIndex = FindEndIndex(dateTimeList, maxDateTime);
            int length = endIndex - startIndex;

            //截取数据放入slicedRecordDictionary
            foreach (var name in dataTypeDictionary.Keys)
            {
                switch (dataTypeDictionary[name])
                {
                    case "bool":
                        slicedBoolDictionary[name] = new bool[length];
                        Array.Copy(fullBoolDictionary[name], startIndex, slicedBoolDictionary[name], 0, length);
                        break;
                    case "int16":
                        slicedInt16Dictionary[name] = new short[length];
                        Array.Copy(fullInt16Dictionary[name], startIndex, slicedInt16Dictionary[name], 0, length);
                        break;
                    case "float32":
                        slicedFloatDictionary[name] = new float[length];
                        Array.Copy(fullFloatDictionary[name], startIndex, slicedFloatDictionary[name], 0, length);
                        break;
                    case "float_int":
                        slicedFloatDictionary[name] = new float[length];
                        Array.Copy(fullFloatDictionary[name], startIndex, slicedFloatDictionary[name], 0, length);
                        break;
                    case "bool_int":
                        slicedBoolDictionary[name] = new bool[length];
                        Array.Copy(fullBoolDictionary[name], startIndex, slicedBoolDictionary[name], 0, length);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported data type: {dataTypeDictionary[name]}");
                }
            }

            //截取日期时间数据并转换为字符用作X轴标签
            slicedTimeLabels = new string[length];
            Array.Copy(fullTimeLabels, startIndex, slicedTimeLabels, 0, length);
            slicedDateLabels = new string[length];
            Array.Copy(fullDateLabels, startIndex, slicedDateLabels, 0, length);

            int FindStartIndex(DateTime[] dateTimeList, DateTime minDateTime)
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

            int FindEndIndex(DateTime[] dateTimeList, DateTime maxDateTime)
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

        /// <summary>
        /// 转换时间日期为字符串用作X轴标签并根据选中的复选框刷新图表
        /// </summary>
        private void RefreshChartSeries()
        {
            ChartSeries = new ObservableCollection<ISeries>();
            cartesianChart.XAxes = new List<Axis>
            {
                new Axis { Labels = slicedTimeLabels },
                new Axis { Labels = slicedDateLabels }
            };

            foreach (CheckBox checkBox in DataSelecter.Items)
            {
                if (checkBox.IsChecked == true)
                {
                    AddChartSeries(checkBox.Content.ToString());
                }
            }
            cartesianChart.Series = ChartSeries;
        }

        private void AddChartSeries(string dataName)
        {
            switch (dataTypeDictionary[dataName])
            {
                case "bool":
                    var boolLineSeries = new LineSeries<bool>
                    {
                        Name = dataName,
                        Values = slicedBoolDictionary[dataName],
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        Stroke = new SolidColorPaint(lineColorsDictionary[dataName]) { StrokeThickness = 1 },
                        LineSmoothness = 0
                    };
                    ChartSeries.Add(boolLineSeries);
                    break;
                case "int16":
                    var int16LineSeries = new LineSeries<short>
                    {
                        Name = dataName,
                        Values = slicedInt16Dictionary[dataName],
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        Stroke = new SolidColorPaint(lineColorsDictionary[dataName]) { StrokeThickness = 1 },
                        LineSmoothness = 0
                    };
                    ChartSeries.Add(int16LineSeries);
                    break;
                case "float32":
                    var floatLineSeries = new LineSeries<float>
                    {
                        Name = dataName,
                        Values = slicedFloatDictionary[dataName],
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        Stroke = new SolidColorPaint(lineColorsDictionary[dataName]) { StrokeThickness = 1 },
                        LineSmoothness = 0
                    };
                    ChartSeries.Add(floatLineSeries);
                    break;
                case "float_int":
                    var floatIntLineSeries = new LineSeries<float>
                    {
                        Name = dataName,
                        Values = slicedFloatDictionary[dataName],
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        Stroke = new SolidColorPaint(lineColorsDictionary[dataName]) { StrokeThickness = 1 },
                        LineSmoothness = 0
                    };
                    ChartSeries.Add(floatIntLineSeries);
                    break;
                case "bool_int":
                    var boolIntLineSeries = new LineSeries<bool>
                    {
                        Name = dataName,
                        Values = slicedBoolDictionary[dataName],
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        Stroke = new SolidColorPaint(lineColorsDictionary[dataName]) { StrokeThickness = 1 },
                        LineSmoothness = 0
                    };
                    ChartSeries.Add(boolIntLineSeries);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported data type: {dataTypeDictionary[dataName]}");
            }

        }

        /// <summary>
        /// 将日期时间控件更新为全部数据的起止时间
        /// </summary>
        private void UpdateDateTimeControl()
        {
            minDateTime = dateTimeList[0];
            maxDateTime = dateTimeList.Last();

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
            UpdateDateTimeControl();
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
