using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ModbusWPF.Models;

namespace ModbusWPF.ViewModel
{
    public class DataPointViewModel
    {
        public ObservableCollection<DataPoint> DataPoints { get; set; }

        public DataPointViewModel()
        {
            DataPoints = new ObservableCollection<DataPoint>();
            LoadDataPointsFromCsv("C:/codes/ModbusWPF/data_points.csv");
        }

        private void LoadDataPointsFromCsv(string filePath)
        {
            var lines = File.ReadAllLines(filePath).Skip(1); // Skip header line
            foreach (var line in lines)
            {
                var values = line.Split(',');
                var dataPoint = new DataPoint(
                    values[0],
                    values[1],
                    values[2],
                    int.Parse(values[3]),
                    int.Parse(values[4]),
                    values[5] == "Y"
                );
                DataPoints.Add(dataPoint);
            }
        }
    }
}