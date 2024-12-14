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
        private Dictionary<string, SKColor> lineColorsDictionary;
        private List<SKColor> lineColors = new List<SKColor>
        {SKColors.Blue, SKColors.Black, SKColors.Yellow, SKColors.Green, SKColors.Red, SKColors.Orange, SKColors.Cyan};

        private Dictionary<string, List<string>> dataRecordDictionary;
        private Dictionary<string, List<string>> fullDataRecordDictionary;
        public ObservableCollection<ISeries> ChartSeries = new ObservableCollection<ISeries>();

        public HisTrendWindow(string hisCSVPath, List<string> dataPointNames)
        {
            InitializeComponent();
            this.filePath = hisCSVPath;
            this.dataPointNames = dataPointNames;

            InitializeData();
            CreateCheckboxes();
            UpdateDateTimeControl();
            RefreshDataAndSeries();
            cartesianChart.Series = ChartSeries;
        }

        private void InitializeData()
        {
            LoadFullDataRecordDictionary();
            lineColorsDictionary = new Dictionary<string, SKColor>();
            for (int i = 0; i < dataPointNames.Count; i++)
            {
                lineColorsDictionary[dataPointNames[i]] = lineColors[i % 7];
            }
        }

        private void CreateCheckboxes()
        {
            var fontSize = (double)FindResource("GlobalFontSize");
            foreach (var name in dataPointNames)
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
            RefreshChartSeriesCollection();
        }

        private void RefreshDataAndSeries()
        {
            dataRecordDictionary = new Dictionary<string, List<string>>();
            var lines = File.ReadAllLines(filePath).Skip(1); // Skip header

            foreach (var line in lines)
            {
                var data = line.Split(',').ToList();
                DateTime dateTime = ParseDateTime(data[0], data[1].Split(".")[0]);

                if (dateTime < minDateTime || dateTime > maxDateTime) continue;

                AddData("date", data[0]);
                AddData("time", data[1].Split(".")[0]);

                for (int i = 0; i < dataPointNames.Count; i++)
                {
                    string dataPointName = dataPointNames[i];
                    AddData(dataPointName, data[i + 2]); // Skip date/time
                }
            }

            RefreshChartSeriesCollection();
        }

        private void AddData(string key, string value)
        {
            if (!dataRecordDictionary.ContainsKey(key))
            {
                dataRecordDictionary[key] = new List<string>();
            }
            dataRecordDictionary[key].Add(value);
        }

        private void RefreshChartSeriesCollection()
        {
            ChartSeries.Clear();
            SetXAxisLabels();

            foreach (CheckBox checkBox in DataSelecter.Items)
            {
                if (checkBox.IsChecked == true)
                {
                    AddChartSeries(checkBox.Content.ToString());
                }
            }
        }

        private void SetXAxisLabels()
        {
            cartesianChart.XAxes = new List<Axis>
            {
                new Axis { Labels = dataRecordDictionary["time"] },
                new Axis { Labels = dataRecordDictionary["date"] }
            };
        }

        private void AddChartSeries(string dataPointName)
        {
            var dataList = dataRecordDictionary[dataPointName];
            List<float> convertedValues = ConvertDataList(dataList);

            var lineSeries = new LineSeries<float>
            {
                Name = dataPointName,
                Values = convertedValues,
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                Stroke = new SolidColorPaint(lineColorsDictionary[dataPointName]) { StrokeThickness = 1 },
                LineSmoothness = 0
            };

            ChartSeries.Add(lineSeries);
        }

        private List<float> ConvertDataList(List<string> dataStringList)
        {
            return dataStringList
                .Select(ConvertStringToFloat)
                .Where(value => !float.IsNaN(value))
                .ToList();
        }

        private float ConvertStringToFloat(string dataString)
        {
            if (float.TryParse(dataString, out float value)) return (float)Math.Round(value, 2);
            return dataString.Equals("true", StringComparison.OrdinalIgnoreCase) ? 1f :
                   dataString.Equals("false", StringComparison.OrdinalIgnoreCase) ? 0f : float.NaN;
        }

        private void UpdateDateTimeControl()
        {
            string firstDateString = fullDataRecordDictionary["date"][0];
            string firstTimeString = fullDataRecordDictionary["time"][0];
            minDateTime = ParseDateTime(firstDateString, firstTimeString);

            string lastDateString = fullDataRecordDictionary["date"].Last();
            string lastTimeString = fullDataRecordDictionary["time"].Last();
            maxDateTime = ParseDateTime(lastDateString, lastTimeString);

            StartDate.SelectedDate = minDateTime.Date;
            StartHour.Text = minDateTime.Hour.ToString("D2");
            StartMinute.Text = minDateTime.Minute.ToString("D2");
            StartSecond.Text = minDateTime.Second.ToString("D2");

            EndDate.SelectedDate = maxDateTime.Date;
            EndHour.Text = maxDateTime.Hour.ToString("D2");
            EndMinute.Text = maxDateTime.Minute.ToString("D2");
            EndSecond.Text = maxDateTime.Second.ToString("D2");
        }

        private DateTime ParseDateTime(string dateString, string timeString)
        {
            var timeParts = timeString.Split(':');
            string hour = timeParts[0], minute = timeParts[1], second = timeParts[2].Split('.')[0];

            DateTime date = DateTime.Parse(dateString);
            return new DateTime(date.Year, date.Month, date.Day, int.Parse(hour), int.Parse(minute), int.Parse(second));
        }

        private void LoadFullDataRecordDictionary()
        {
            fullDataRecordDictionary = new Dictionary<string, List<string>>();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines.Skip(1))
            {
                var data = line.Split(',').ToList();
                DateTime dateTime = ParseDateTime(data[0], data[1].Split(".")[0]);

                AddDataToFullRecord("date", data[0]);
                AddDataToFullRecord("time", data[1].Split(".")[0]);

                for (int i = 0; i < dataPointNames.Count; i++)
                {
                    AddDataToFullRecord(dataPointNames[i], data[i + 2]);
                }
            }
        }

        private void AddDataToFullRecord(string key, string value)
        {
            if (!fullDataRecordDictionary.ContainsKey(key))
            {
                fullDataRecordDictionary[key] = new List<string>();
            }
            fullDataRecordDictionary[key].Add(value);
        }

        private void QueryBtnClicked(object sender, EventArgs e)
        {
            if (ValidateAndParseDateTime())
            {
                RefreshDataAndSeries();
            }
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

        private void RefreshBtnClicked(object sender, RoutedEventArgs e)
        {
            LoadFullDataRecordDictionary();
            UpdateDateTimeControl();
            RefreshDataAndSeries();
        }
    }
}
