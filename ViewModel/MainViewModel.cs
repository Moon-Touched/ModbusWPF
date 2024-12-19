using ModbusWPF.Models;
using OxyPlot;
using OxyPlot.Series;
using System.IO;

namespace ModbusWPF.ViewModel
{
    public class MainViewModel
    {
        public DataPointViewModel DataPointViewModel { get; set; }
        public RealTimePlotViewModel RealTimePlotViewModel { get; set; }
        public readonly object recordLock = new object();

        public string dataCSVPath;
        public string portCSVPath;
        public string hisCSVPath;

        private Dictionary<string, OxyColor> lineColorsDictionary;
        private List<OxyColor> lineColors = new List<OxyColor>
        {OxyColors.Blue, OxyColors.Black, OxyColors.Yellow, OxyColors.Green, OxyColors.Red, OxyColors.Orange, OxyColors.Cyan};
        public MainViewModel(string basePath)
        {
            dataCSVPath = Path.Combine(basePath, "data_points.csv");
            portCSVPath = Path.Combine(basePath, "port_info.csv");
            hisCSVPath = Path.Combine(basePath, $"DataRecord_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.Create(hisCSVPath).Close();
            DataPointViewModel = new DataPointViewModel(dataCSVPath, portCSVPath, recordLock);
            RealTimePlotViewModel = new RealTimePlotViewModel();
            InitializeLineSreies();
        }

        public void InitializeLineSreies()
        {
            lineColorsDictionary = new Dictionary<string, OxyColor>();
            for (int i = 0; i < DataPointViewModel.DataPointsDictionary.Keys.Count; i++)
            {
                lineColorsDictionary[DataPointViewModel.DataPointsDictionary.Keys.ToList()[i]] = lineColors[i % lineColors.Count];
            }

            foreach (var dataPoint in DataPointViewModel.DataPointsDictionary.Values)
            {
                var lineSeries = new LineSeries
                {
                    Title = dataPoint.Name,
                    Color = lineColorsDictionary[dataPoint.Name],
                };
                RealTimePlotViewModel.PlotModel.Series.Add(lineSeries);
                RealTimePlotViewModel.LineSeriesDictionary.Add(dataPoint.Name, lineSeries);
            }
        }

        public async void RecordData(int sampleTimeMillisecond)
        {
            File.WriteAllText(hisCSVPath, $"date,time,{string.Join(",", DataPointViewModel.DataPointsDictionary.Keys)}\n");
            while (true)
            {
                List<string> valueStrings = new List<string>();
                var dateTime = DateTime.Now;
                foreach (var dataPoint in DataPointViewModel.DataPointsDictionary.Values)
                {
                    switch (dataPoint)
                    {
                        case BoolDataPoint boolDataPoint:
                            valueStrings.Add(boolDataPoint.Value.ToString());
                            AddPointToSeries(dataPoint.Name, dateTime, boolDataPoint.Value ? 1 : 0);
                            break;
                        case Int16DataPoint int16DataPoint:
                            valueStrings.Add(int16DataPoint.Value.ToString());
                            AddPointToSeries(dataPoint.Name, dateTime, int16DataPoint.Value);
                            break;
                        case Float32DataPoint float32DataPoint:
                            valueStrings.Add(float32DataPoint.Value.ToString());
                            AddPointToSeries(dataPoint.Name, dateTime, float32DataPoint.Value);
                            break;
                        case FloatIntDataPoint floatIntDataPoint:
                            valueStrings.Add(floatIntDataPoint.Value.ToString());
                            AddPointToSeries(dataPoint.Name, dateTime, floatIntDataPoint.Value);
                            break;
                        case BoolIntDataPoint boolIntDataPoint:
                            valueStrings.Add(boolIntDataPoint.Value.ToString());
                            AddPointToSeries(dataPoint.Name, dateTime, boolIntDataPoint.Value ? 1 : 0);
                            break;
                    }
                }


                lock (recordLock)
                {
                    using (var writer = new StreamWriter(hisCSVPath, true))
                    {
                        writer.WriteLine($"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss.fff},{string.Join(",", valueStrings)}");
                    }
                }

                RealTimePlotViewModel.PlotModel.InvalidatePlot(true);

                await Task.Delay(sampleTimeMillisecond);
            }
        }
        private void AddPointToSeries(string dataPointName, DateTime dateTime, double value)
        {
            var lineSeries = RealTimePlotViewModel.LineSeriesDictionary[dataPointName];

            // Add the new data point
            lineSeries.Points.Add(new DataPoint(dateTime.ToOADate() + 1, value));

            if (lineSeries.Points.Count > 10000)
            {
                lineSeries.Points.RemoveAt(0);
            }
        }
    }
}
