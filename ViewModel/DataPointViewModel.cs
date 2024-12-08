using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using ModbusWPF.Models;
using ModbusWPF.Helper;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;

namespace ModbusWPF.ViewModel
{
    public class DataPointViewModel
    {
        public ModBusHelper ModbusHelper { get; set; }
        public Dictionary<string, DataPointBase> DataPointsDictionary { get; set; }
        private Stack<(string taskType, string dataName)> taskStack;

        public DataPointViewModel()
        {
            //DataPointsDictionary的key是数据点的名字，value是数据点对象
            DataPointsDictionary = new Dictionary<string, DataPointBase>();
            ModbusHelper = new ModBusHelper();
            //taskType使用"R","W"表示读写,dataName用于从DataPointsDictionary中获取数据点对象
            taskStack = new Stack<(string taskType, string dataName)>();
            LoadDataPointsFromCsv("C:/codes/ModbusWPF/data_points.csv");
        }

        private void LoadDataPointsFromCsv(string filePath)
        {
            var lines = File.ReadAllLines(filePath).Skip(1); // 跳过表头
            foreach (var line in lines)
            {
                var info = line.Split(',');
                string name = info[0];
                string dataType = info[1];
                string portName = info[2];
                int slaveAddress = int.Parse(info[3]);
                int registerAddress = int.Parse(info[4]);
                bool readOnly = info[5] == "Y";

                switch (dataType)
                {
                    case "bool":
                        BoolDataPoint boolDataPoint = new BoolDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, false);
                        ModbusHelper.ReadBoolData(boolDataPoint);
                        boolDataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                        DataPointsDictionary[name] = boolDataPoint;
                        break;
                    case "int16":
                        Int16DataPoint int16DataPoint = new Int16DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0);
                        ModbusHelper.ReadInt16Data(int16DataPoint);
                        int16DataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                        DataPointsDictionary[name] = int16DataPoint;
                        break;
                    case "float32":
                        Float32DataPoint float32DataPoint = new Float32DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f);
                        ModbusHelper.ReadFloat32Data(float32DataPoint);
                        float32DataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                        DataPointsDictionary[name] = float32DataPoint;
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported data type: {dataType}");
                };
            }
        }

        public void ProcessTaskQueue()
        {
            if (taskStack.Count == 0)
            {
                // 如果队列为空，添加所有读取任务
                foreach (var dataName in DataPointsDictionary.Keys)
                {
                    taskStack.Push(("R", dataName));
                }
            }
            else
            {
                // 从队列中取出任务
                var (taskType, dataName) = taskStack.Pop();
                var dataPoint = DataPointsDictionary[dataName];
                if (taskType == "R")
                {
                    switch (dataPoint.DataType)
                    {
                        case "bool":
                            ModbusHelper.ReadBoolData((BoolDataPoint)dataPoint);
                            break;
                        case "int16":
                            ModbusHelper.ReadInt16Data((Int16DataPoint)dataPoint);
                            break;
                        case "float32":
                            ModbusHelper.ReadFloat32Data((Float32DataPoint)dataPoint);
                            break;
                    }
                    OnPropertyChanged(dataName);
                }
                else if (taskType == "W")
                {
                    switch (dataPoint.DataType)
                    {
                        case "bool":
                            ModbusHelper.WriteBoolData((BoolDataPoint)dataPoint);
                            break;
                        case "int16":
                            ModbusHelper.WriteInt16Data((Int16DataPoint)dataPoint);
                            break;
                        case "float32":
                            ModbusHelper.WriteFloat32Data((Float32DataPoint)dataPoint);
                            break;
                    }
                    // 写入完成后，立即添加一个读取任务以确认成功写入。
                    // 如果写入失败，则仍显示原来的值
                    taskStack.Push(("R", dataName));
                }
            }
        }

        private void DataPointPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            DataPointBase dataPoint = (DataPointBase)sender; 
            // 将写入任务加入栈顶
            taskStack.Push(("W", dataPoint.Name));
        }

    }
}