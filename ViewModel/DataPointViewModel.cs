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

        public DataPointViewModel()
        {
            DataPointsDictionary = new Dictionary<string, DataPointBase>();
            ModbusHelper = new ModBusHelper();
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
        private void DataPointPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            DataPointBase dataPoint = (DataPointBase)sender; // 获取触发事件的对象

            if (!dataPoint.ReadOnly) // 判断是否为只读
            {
                switch (dataPoint.DataType)
                {
                    case "bool":
                        BoolDataPoint boolDataPoint = (BoolDataPoint)sender;
                        ModbusHelper.WriteBoolData(boolDataPoint, boolDataPoint.Value);
                        break;
                    case "int16":
                        Int16DataPoint int16DataPoint = (Int16DataPoint)sender;
                        ModbusHelper.WriteInt16Data(int16DataPoint, (ushort)int16DataPoint.Value);
                        break;
                    case "float32":
                        Float32DataPoint float32DataPoint = (Float32DataPoint)sender;
                        ModbusHelper.WriteFloat32Data(float32DataPoint, float32DataPoint.Value);
                        break;
                }
            }
        }

    }
}