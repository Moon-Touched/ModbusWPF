using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using ModbusWPF.Models;
using ModbusWPF.Helper;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Threading;

namespace ModbusWPF.ViewModel
{
    public class DataPointViewModel
    {
        public ModBusHelper ModbusHelper { get; set; }
        public Dictionary<string, DataPointBase> DataPointsDictionary { get; set; }
        private Stack<(string taskType, string dataName)> taskStack;

        public DataPointViewModel(string dataCSVPath,string portCSVPath)
        {
            //DataPointsDictionary��key�����ݵ�����֣�value�����ݵ����
            DataPointsDictionary = new Dictionary<string, DataPointBase>();
            ModbusHelper = new ModBusHelper(portCSVPath);
            //taskTypeʹ��"R","W"��ʾ��д,dataName���ڴ�DataPointsDictionary�л�ȡ���ݵ����
            taskStack = new Stack<(string taskType, string dataName)>();
            LoadDataPointsFromCsv(dataCSVPath);
        }

        private void LoadDataPointsFromCsv(string dataCSVPath)
        {
            var lines = File.ReadAllLines(dataCSVPath).Skip(1); // ������ͷ
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
                    // �������Ϊ�գ�������ж�ȡ����
                    foreach (var dataName in DataPointsDictionary.Keys)
                    {
                           taskStack.Push(("R", dataName));
                    }
                }
                else
                {
                    // �Ӷ�����ȡ������
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
                        // д����ɺ��������һ����ȡ������ȷ�ϳɹ�д�롣
                        // ���д��ʧ�ܣ�������ʾԭ����ֵ
                        taskStack.Push(("R", dataName));
                    }
                    await Task.Delay(delayMilliseconds);
                }
            }
        }

        private void DataPointPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            DataPointBase dataPoint = (DataPointBase)sender;
            // ��д���������ջ��
            if (!dataPoint.ReadOnly)
            {
                taskStack.Push(("W", dataPoint.Name));
            }
        }

    }
}