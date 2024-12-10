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

namespace ModbusWPF.ViewModel
{
    public class DataPointViewModel
    {
        public ModBusHelper ModbusHelper { get; set; }
        public Dictionary<string, DataPointBase> DataPointsDictionary { get; set; }
        private Stack<(string taskType, string dataName)> taskStack;

        public DataPointViewModel(string dataCSVPath,string portCSVPath)
        {
            //DataPointsDictionary的key是数据点的名字，value是数据点对象
            DataPointsDictionary = new Dictionary<string, DataPointBase>();
            ModbusHelper = new ModBusHelper(portCSVPath);
            //taskType使用"R","W"表示读写,dataName用于从DataPointsDictionary中获取数据点对象
            taskStack = new Stack<(string taskType, string dataName)>();
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

                switch (dataType)
                {
                    case "bool":
                        BoolDataPoint boolDataPoint = new BoolDataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, false);
                        ModbusHelper.ReadBoolData(boolDataPoint);
                        if (boolDataPoint.ReadOnly == false)
                        {
                            boolDataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                        }
                        DataPointsDictionary[name] = boolDataPoint;
                        break;
                    case "int16":
                        Int16DataPoint int16DataPoint = new Int16DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0);
                        ModbusHelper.ReadInt16Data(int16DataPoint);
                        if (int16DataPoint.ReadOnly == false)
                        {
                            int16DataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                        }
                        DataPointsDictionary[name] = int16DataPoint;
                        break;
                    case "float32":
                        Float32DataPoint float32DataPoint = new Float32DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly, 0.1f);
                        ModbusHelper.ReadFloat32Data(float32DataPoint);
                        if (float32DataPoint.ReadOnly == false)
                        {
                            float32DataPoint.PropertyChanged += DataPointPropertyChangedHandler;
                        }
                        DataPointsDictionary[name] = float32DataPoint;
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported data type: {dataType}");
                };
            }
        }

        public async void ProcessTaskQueue(int delayMilliseconds)
        {
            while (true)
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
                                var boolDataPoint = (BoolDataPoint)dataPoint;
                                ModbusHelper.ReadBoolData(boolDataPoint);
                                break;
                            case "int16":
                                var int16DataPoint = (Int16DataPoint)dataPoint;
                                ModbusHelper.ReadInt16Data(int16DataPoint);
                                break;
                            case "float32":
                                var float32DataPoint = (Float32DataPoint)dataPoint;
                                ModbusHelper.ReadFloat32Data((Float32DataPoint)dataPoint);
                                break;
                            case "int_float":
                                var intFloatDataPoint = (IntFloatDataPoint)dataPoint; 
                                ModbusHelper.ReadInt16Data(intFloatDataPoint);
                                break;
                        }

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
                    await Task.Delay(delayMilliseconds);
                }
            }
        }

        private void DataPointPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            DataPointBase dataPoint = (DataPointBase)sender;
            // 将写入任务加入栈顶
            if (!dataPoint.ReadOnly)
            {
                taskStack.Push(("W", dataPoint.Name));
            }
        }

    }
}