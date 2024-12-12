using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using ModbusWPF.Models;
using ModbusWPF.Helper;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Threading;
using static ModbusWPF.Models.Float32DataPoint;
using System.Diagnostics;

namespace ModbusWPF.ViewModel
{
    public class DataPointViewModel
    {
        public ModBusHelper ModbusHelper { get; set; }
        public Dictionary<string, DataPointBase> DataPointsDictionary { get; set; }
        private Stack<(string taskType, string dataName)> taskStack;

        public DataPointViewModel(string dataCSVPath, string portCSVPath)
        {
            DataPointsDictionary = new Dictionary<string, DataPointBase>();
            ModbusHelper = new ModBusHelper(portCSVPath);
            taskStack = new Stack<(string taskType, string dataName)>();
            LoadDataPointsFromCsv(dataCSVPath);
        }

        private void LoadDataPointsFromCsv(string dataCSVPath)
        {
            var lines = File.ReadAllLines(dataCSVPath).Skip(1); // Ìø¹ý±íÍ·
            foreach (var line in lines)
            {
                var info = line.Split(',');
                string name = info[0];
                string dataType = info[1];
                string portName = info[2];
                int slaveAddress = int.Parse(info[3]);
                int registerAddress = int.Parse(info[4]);
                bool readOnly = info[5] == "Y";

                DataPointBase dataPoint = dataType switch
                {
                    "bool" => new BoolDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, false),
                    "int16" => new Int16DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0),
                    "float32" => new Float32DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f),
                    "float_int" => new FloatIntDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f),
                    "bool_int" => new BoolIntDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, false),
                    _ => throw new InvalidOperationException($"Unsupported data type: {dataType}")
                };

                ModbusHelper.ReadData(dataPoint);
                if (!dataPoint.ReadOnly)
                {
                    dataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                }
                DataPointsDictionary[name] = dataPoint;
            }
        }

        public async void ProcessTaskQueue(int delayMilliseconds)
        {
            while (true)
            {
                if (taskStack.Count == 0)
                {
                    foreach (var dataName in DataPointsDictionary.Keys)
                    {
                        taskStack.Push(("R", dataName));
                    }
                }
                else
                {
                    var (taskType, dataName) = taskStack.Pop();
                    var dataPoint = DataPointsDictionary[dataName];

                    if (taskType == "R")
                    {
                        ModbusHelper.ReadData(dataPoint);
                    }
                    else if (taskType == "W")
                    {
                        ModbusHelper.WriteData(dataPoint);
                        taskStack.Push(("R", dataName));
                    }
                    await Task.Delay(delayMilliseconds);
                }
            }
        }

        private void DataPointPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            DataPointBase dataPoint = (DataPointBase)sender;
            if (!dataPoint.ReadOnly)
            {
                taskStack.Push(("W", dataPoint.Name));
            }
        }

        public async void RecordData(string filePath, int sampleTimeMillisecond)
        {
            File.WriteAllText(filePath, $"date,time,{string.Join(",", DataPointsDictionary.Keys)}\n");
            while (true)
            {
                var values = DataPointsDictionary.Values.Select(dp => dp switch
                {
                    BoolDataPoint boolDp => boolDp.Value.ToString(),
                    Int16DataPoint int16Dp => int16Dp.Value.ToString(),
                    Float32DataPoint float32Dp => float32Dp.Value.ToString(),
                    FloatIntDataPoint floatIntDp => floatIntDp.Value.ToString(),
                    BoolIntDataPoint boolIntDp => boolIntDp.Value.ToString(),
                    _ => throw new InvalidOperationException($"Unsupported data type: {dp.DataType}")
                });

                using (var writer = new StreamWriter(filePath, true))
                {
                    var dateTime = DateTime.Now;
                    writer.WriteLine($"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss.fff},{string.Join(",", values)}");
                }

                await Task.Delay(sampleTimeMillisecond);
            }
        }
    }
}
