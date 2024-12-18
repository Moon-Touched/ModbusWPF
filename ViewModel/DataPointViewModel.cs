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
using System.Xml.Linq;
using System.Diagnostics.Metrics;

namespace ModbusWPF.ViewModel
{
    public class DataPointViewModel
    {
        public ModBusHelper ModbusHelper { get; set; }
        public Dictionary<string, DataPointBase> DataPointsDictionary { get; set; }
        public Dictionary<string, Stack<(string taskType, DataPointBase dataPoint)>> TaskStackDictionary { get; set; }

        public readonly object RecordLock = new object();

        public DataPointViewModel(string dataCSVPath, string portCSVPath)
        {
            ModbusHelper = new ModBusHelper(portCSVPath);
            DataPointsDictionary = new Dictionary<string, DataPointBase>();
            TaskStackDictionary = new Dictionary<string, Stack<(string taskType, DataPointBase dataPoint)>>();
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

                DataPointBase dataPoint;
                switch (dataType)
                {
                    case "bool":
                        dataPoint = new BoolDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, false);
                        break;
                    case "int16":
                        dataPoint = new Int16DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0);
                        break;
                    case "float32":
                        dataPoint = new Float32DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f);
                        break;
                    case "float_int":
                        dataPoint = new FloatIntDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f);
                        break;
                    case "bool_int":
                        dataPoint = new BoolIntDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, false);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported data type: {dataType}");
                }

                ModbusHelper.ReadData(dataPoint);
                if (!dataPoint.ReadOnly)
                {
                    dataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                }
                DataPointsDictionary[name] = dataPoint;
            }

        }

        public void StartTasks(int delayMilliseconds)
        {
            foreach (var portName in ModbusHelper.ModbusMasterDictionary.Keys)
            {
                TaskStackDictionary[portName] = new Stack<(string taskType, DataPointBase dataPoint)>();
                ProcessTaskQueue(portName, delayMilliseconds);
            }
        }

        public async void ProcessTaskQueue(string portName, int delayMilliseconds)
        {
            var taskStack = TaskStackDictionary[portName];
            while (true)
            {
                if (taskStack.Count == 0)
                {
                    foreach (var dataPoint in DataPointsDictionary.Values)
                    {
                        if (dataPoint.PortName == portName)
                        {
                            taskStack.Push(("R", dataPoint));
                        }
                    }
                }
                else
                {
                    var (taskType, dataPoint) = taskStack.Pop();

                    if (taskType == "R")
                    {
                        ModbusHelper.ReadData(dataPoint);
                    }
                    else if (taskType == "W")
                    {
                        ModbusHelper.WriteData(dataPoint);
                        taskStack.Push(("R", dataPoint));
                    }
                    await Task.Delay(delayMilliseconds);
                }
            }
        }

        private void DataPointPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            var dataPoint = (DataPointBase)sender;
            if (!dataPoint.ReadOnly)
            {
                TaskStackDictionary[dataPoint.PortName].Push(("W", dataPoint));
            }
        }

        public async void RecordData(string filePath, int sampleTimeMillisecond)
        {
            File.WriteAllText(filePath, $"date,time,{string.Join(",", DataPointsDictionary.Keys)}\n");
            while (true)
            {
                List<string> values = new List<string>();
                lock (RecordLock)
                {
                    foreach (var dataPoint in DataPointsDictionary.Values)
                    {
                        switch (dataPoint)
                        {
                            case BoolDataPoint boolDataPoint:
                                values.Add(boolDataPoint.Value.ToString());
                                break;
                            case Int16DataPoint int16DataPoint:
                                values.Add(int16DataPoint.Value.ToString());
                                break;
                            case Float32DataPoint float32DataPoint:
                                values.Add(float32DataPoint.Value.ToString());
                                break;
                            case FloatIntDataPoint floatIntDataPoint:
                                values.Add(floatIntDataPoint.Value.ToString());
                                break;
                            case BoolIntDataPoint boolIntDataPoint:
                                values.Add(boolIntDataPoint.Value.ToString());
                                break;
                        }
                    }

                    var dateTime = DateTime.Now;

                    lock (RecordLock)
                    {
                        using (var writer = new StreamWriter(filePath, true))
                        {
                            writer.WriteLine($"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss.fff},{string.Join(",", values)}");
                        }
                    }
                }

                await Task.Delay(sampleTimeMillisecond);
            }
        }
    }
}
