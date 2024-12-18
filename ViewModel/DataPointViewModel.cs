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
using System.Text;

namespace ModbusWPF.ViewModel
{
    public class DataPointViewModel
    {
        public ModBusHelper modbusHelper;
        public Dictionary<string, DataPointBase> DataPointsDictionary { get; set; }
        public Dictionary<string, Stack<(string taskType, DataPointBase dataPoint)>> taskStackDictionary;

        public readonly object RecordLock = new object();

        public int boolCount;
        public int int16Count;
        public int float32Count;
        public Dictionary<int, string> BoolDataIndexDictionary;
        public Dictionary<int, string> Int16DataIndexDictionary;
        public Dictionary<int, string> Float32DataIndexDictionary;

        public DataPointViewModel(string dataCSVPath, string portCSVPath)
        {
            modbusHelper = new ModBusHelper(portCSVPath);
            DataPointsDictionary = new Dictionary<string, DataPointBase>();
            taskStackDictionary = new Dictionary<string, Stack<(string taskType, DataPointBase dataPoint)>>();

            boolCount = 0;
            int16Count = 0;
            float32Count = 0;
            BoolDataIndexDictionary = new Dictionary<int, string>();
            Int16DataIndexDictionary = new Dictionary<int, string>();
            Float32DataIndexDictionary = new Dictionary<int, string>();

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
                        BoolDataIndexDictionary[boolCount] = name;
                        boolCount++;
                        break;
                    case "int16":
                        dataPoint = new Int16DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0);
                        Int16DataIndexDictionary[int16Count] = name;
                        int16Count++;
                        break;
                    case "float32":
                        dataPoint = new Float32DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f);
                        Float32DataIndexDictionary[float32Count] = name;
                        float32Count++;
                        break;
                    case "float_int":
                        dataPoint = new FloatIntDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f);
                        Float32DataIndexDictionary[float32Count] = name;
                        float32Count++;
                        break;
                    case "bool_int":
                        dataPoint = new BoolIntDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, false);
                        BoolDataIndexDictionary[boolCount] = name;
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
                DataPointsDictionary[name] = dataPoint;
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
            File.WriteAllText(hisCSVPath, $"date,time,{string.Join(",", DataPointsDictionary.Keys)}\n",Encoding.UTF8);
            while (true)
            {
                var valueStrings = new List<string>();
                var boolValues = new List<bool>();
                var floatValues = new List<float>();
                var intValues = new List<short>();
                foreach (var dataPoint in DataPointsDictionary.Values)
                {
                    switch (dataPoint)
                    {
                        case BoolDataPoint boolDataPoint:
                            var boolValeu = boolDataPoint.Value;
                            valueStrings.Add(boolValeu.ToString());
                            boolValues.Add(boolValeu);
                            break;
                        case Int16DataPoint int16DataPoint:
                            var intValue = int16DataPoint.Value;
                            valueStrings.Add(intValue.ToString());
                            intValues.Add(intValue);
                            break;
                        case Float32DataPoint float32DataPoint:
                            var floatValue = float32DataPoint.Value;
                            valueStrings.Add(floatValue.ToString());
                            floatValues.Add(floatValue);
                            break;
                        case FloatIntDataPoint floatIntDataPoint:
                            var floatValue2 = floatIntDataPoint.Value;
                            valueStrings.Add(floatValue2.ToString());
                            floatValues.Add(floatValue2);
                            break;
                        case BoolIntDataPoint boolIntDataPoint:
                            var boolValue2 = boolIntDataPoint.Value;
                            valueStrings.Add(boolValue2.ToString());
                            boolValues.Add(boolValue2);
                            break;
                    }
                }

                var dateTime = DateTime.Now;
                using (var writer = new StreamWriter(hisCSVPath, true, Encoding.UTF8))
                {
                    writer.WriteLine($"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss.fff},{string.Join(",", valueStrings)}", Encoding.UTF8);
                    //Debug.WriteLine($"{dateTime:yyyy-MM-dd},{dateTime:HH:mm:ss.fff},{string.Join(",", valueStrings)}");
                }

                lock (RecordLock)
                {
                    using (var binaryWriter = new BinaryWriter(File.Open(hisBinaryPath, FileMode.Append)))
                    {
                        // 写入DateTime，64位
                        binaryWriter.Write(dateTime.Ticks);
                        //Debug.WriteLine("**************************************************************************");
                        //Debug.WriteLine($"DateTime: {dateTime.Ticks}");

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
