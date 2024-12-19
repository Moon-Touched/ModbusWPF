using ModbusWPF.Helper;
using ModbusWPF.Models;
using System.ComponentModel;
using System.IO;

namespace ModbusWPF.ViewModel
{
    public class DataPointViewModel
    {
        private ModBusHelper modbusHelper;
        public Dictionary<string, DataPointBase> DataPointsDictionary { get; set; }
        private Dictionary<string, Stack<(string taskType, DataPointBase dataPoint)>> taskStackDictionary;

        public DataPointViewModel(string dataCSVPath, string portCSVPath, object recordLock)
        {
            modbusHelper = new ModBusHelper(portCSVPath);
            DataPointsDictionary = new Dictionary<string, DataPointBase>();
            taskStackDictionary = new Dictionary<string, Stack<(string taskType, DataPointBase dataPoint)>>();
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

    }
}
