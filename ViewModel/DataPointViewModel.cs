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
        public ModBusHelper modbusHelper;
        public Dictionary<string, DataPointBase> dataPointsDictionary;
        public Dictionary<string, Stack<(string taskType, DataPointBase dataPoint)>> taskStackDictionary;

        public readonly object RecordLock = new object();

        public int boolCount;
        public int int16Count;
        public int float32Count;
        public Dictionary<string, int> dataIndexDictionary;

        public DataPointViewModel(string dataCSVPath, string portCSVPath)
        {
            modbusHelper = new ModBusHelper(portCSVPath);
            dataPointsDictionary = new Dictionary<string, DataPointBase>();
            taskStackDictionary = new Dictionary<string, Stack<(string taskType, DataPointBase dataPoint)>>();

            boolCount = 0;
            int16Count = 0;
            float32Count = 0;
            dataIndexDictionary = new Dictionary<string, int>();

            LoadDataPointsFromCsv(dataCSVPath);
        }

        private void LoadDataPointsFromCsv(string dataCSVPath)
        {
            var lines = File.ReadAllLines(dataCSVPath).Skip(1); // 跳过表头
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
                        dataIndexDictionary[name] = boolCount;
                        boolCount++;
                        break;
                    case "int16":
                        dataPoint = new Int16DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0);
                        dataIndexDictionary[name] = int16Count;
                        int16Count++;
                        break;
                    case "float32":
                        dataPoint = new Float32DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f);
                        dataIndexDictionary[name] = float32Count;
                        float32Count++;
                        break;
                    case "float_int":
                        dataPoint = new FloatIntDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f);
                        dataIndexDictionary[name] = float32Count;
                        float32Count++;
                        break;
                    case "bool_int":
                        dataPoint = new BoolIntDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, false);
                        dataIndexDictionary[name] = boolCount;
                        boolCount++;
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported data type: {dataType}");
                }

                modbusHelper.ReadData(dataPoint);
                if (!dataPoint.ReadOnly)
                {
                    dataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                }
                dataPointsDictionary[name] = dataPoint;
            }

        }

        public void StartTasks(int delayMilliseconds)
        {
            foreach (var portName in modbusHelper.ModbusMasterDictionary.Keys)
            {
                taskStackDictionary[portName] = new Stack<(string taskType, DataPointBase dataPoint)>();
                ProcessTaskQueue(portName, delayMilliseconds);
            }
        }

        public async void ProcessTaskQueue(string portName, int delayMilliseconds)
        {
            var taskStack = taskStackDictionary[portName];
            while (true)
            {
                if (taskStack.Count == 0)
                {
                    foreach (var dataPoint in dataPointsDictionary.Values)
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
                        modbusHelper.ReadData(dataPoint);
                    }
                    else if (taskType == "W")
                    {
                        modbusHelper.WriteData(dataPoint);
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
                taskStackDictionary[dataPoint.PortName].Push(("W", dataPoint));
            }
        }

        public async void RecordData(string hisCSVPath, string hisBinaryPath, int sampleTimeMillisecond)
        {
            File.Create(hisBinaryPath).Close();
            File.WriteAllText(hisCSVPath, $"date,time,{string.Join(",", dataPointsDictionary.Keys)}\n");
            while (true)
            {
                var values = new List<string>();
                DataRecord dataRecord;
                var boolValues = new List<bool>();
                var floatValues = new List<float>();
                var intValues = new List<short>();
                foreach (var dataPoint in dataPointsDictionary.Values)
                {
                    switch (dataPoint)
                    {
                        case BoolDataPoint boolDataPoint:
                            var boolValeu = boolDataPoint.Value;
                            values.Add(boolValeu.ToString());
                            boolValues.Add(boolValeu);
                            break;
                        case Int16DataPoint int16DataPoint:
                            var intValue = int16DataPoint.Value;
                            values.Add(intValue.ToString());
                            intValues.Add(intValue);
                            break;
                        case Float32DataPoint float32DataPoint:
                            var floatValue = float32DataPoint.Value;
                            values.Add(floatValue.ToString());
                            floatValues.Add(floatValue);
                            break;
                        case FloatIntDataPoint floatIntDataPoint:
                            var floatValue2 = floatIntDataPoint.Value;
                            values.Add(floatValue2.ToString());
                            floatValues.Add(floatValue2);
                            break;
                        case BoolIntDataPoint boolIntDataPoint:
                            var boolValue2 = boolIntDataPoint.Value;
                            values.Add(boolValue2.ToString());
                            boolValues.Add(boolValue2);
                            break;
                    }

                    var dateTime = DateTime.Now;
                    using (var writer = new StreamWriter(hisCSVPath, true))
                    {
                        writer.WriteLine($"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss.fff},{string.Join(",", values)}");
                    }

                    using (var binaryWriter = new BinaryWriter(File.Open(hisBinaryPath, FileMode.Append)))
                    {
                        lock (RecordLock)
                        {
                            // 写入DateTime，64位
                            binaryWriter.Write(dateTime.Ticks);

                            foreach (var boolValue in boolValues)
                            {
                                binaryWriter.Write(boolValue);
                            }

                            foreach (var intValue in intValues)
                            {
                                binaryWriter.Write(intValue);
                            }

                            foreach (var floatValue in floatValues)
                            {
                                binaryWriter.Write(floatValue);
                            }
                        }
                    }

                    await Task.Delay(sampleTimeMillisecond);
                }
            }
        }
    }
}
