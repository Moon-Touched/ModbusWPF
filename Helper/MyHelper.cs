using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;
using NModbus.IO;
using ModbusWPF.Models;
using NModbus.Device;
using NModbus.Extensions.Enron;
using System.Diagnostics;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;

namespace ModbusWPF.Helper
{
    public class ModBusHelper
    {
        private readonly Dictionary<string, SerialPort> SerialPortDictionary = new();
        private readonly Dictionary<string, IModbusMaster> ModbusMasterDictionary = new();

        public ModBusHelper(string portCSVPath)
        {
            InitializeSerialPorts(portCSVPath);
        }

        private void InitializeSerialPorts(string portCSVPath)
        {
            var lines = File.ReadAllLines(portCSVPath).Skip(1); // 跳过表头
            foreach (var line in lines)
            {
                var info = line.Split(',');
                string portName = info[0];
                int baudRate = int.Parse(info[1]);
                int parity = int.Parse(info[4]);
                int dataBits = int.Parse(info[2]);
                int stopBits = int.Parse(info[3]);

                var port = new SerialPort(portName, baudRate, (Parity)parity, dataBits, (StopBits)stopBits);
                port.ReadTimeout = 1000;
                port.WriteTimeout = 1000;

                SerialPortDictionary[portName] = port;
                try
                {
                    if (!port.IsOpen)
                    {
                        port.Open();
                    }
                    var factory = new ModbusFactory();
                    ModbusMasterDictionary[portName] = factory.CreateRtuMaster(new SerialPortAdapter(port));
                }

                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开串口 {portName}: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //向从机发送读取请求并自动更新DataPoint
        public void ReadBoolData(BoolDataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            bool coilStatus = master.ReadCoils(slaveAddress, registerAddress, 1)[0];
            dataPoint.Value = coilStatus;
            //Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteBoolData(BoolDataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            master.WriteSingleCoil(slaveAddress, registerAddress, dataPoint.Value);
        }

        public void ReadInt16Data(Int16DataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort intValue = master.ReadHoldingRegisters(slaveAddress, registerAddress, 1)[0];
            dataPoint.Value = (short)intValue;
            //Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteInt16Data(Int16DataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;
            master.WriteSingleRegister(slaveAddress, registerAddress, (ushort)dataPoint.Value);
        }

        public void ReadFloat32Data(Float32DataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort[] registersFloat32 = master.ReadHoldingRegisters(slaveAddress, registerAddress, 2);
            float floatValue = BitConverter.ToSingle(BitConverter.GetBytes(registersFloat32[0] << 16 | registersFloat32[1]), 0); ;
            dataPoint.Value = floatValue;
            //Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteFloat32Data(Float32DataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;
            var bytes = BitConverter.GetBytes(dataPoint.Value);
            ushort[] data = { BitConverter.ToUInt16(bytes, 2), BitConverter.ToUInt16(bytes, 0) };
            //Debug.WriteLine(dataPoint.Name + " Write:" + data[0] +" "+ data[1]);
            master.WriteMultipleRegisters(slaveAddress, registerAddress, data);
        }

        public void ReadFloatIntData(FloatIntDataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort intValue = master.ReadHoldingRegisters(slaveAddress, registerAddress, 1)[0];
            dataPoint.Value = intValue / 10f;
            Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteFloatIntData(FloatIntDataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            //Debug.WriteLine(dataPoint.Name + ":" + (ushort)(dataPoint.Value * 10));
            master.WriteSingleRegister(slaveAddress, registerAddress, (ushort)(dataPoint.Value * 10));
        }

        public void ReadIntBoolData(BoolIntDataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort intValue = master.ReadHoldingRegisters(slaveAddress, registerAddress, 1)[0];
            dataPoint.Value = (intValue ==1);
            //Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteIntBoolData(BoolIntDataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            //Debug.WriteLine(dataPoint.Name + " Write:" + dataPoint.Value);
            if (dataPoint.Value)
            {
                master.WriteSingleRegister(slaveAddress, registerAddress, 1);
            }
            else
            {
                master.WriteSingleRegister(slaveAddress, registerAddress, 0);
            }
        }

        public void Close()
        {
            foreach (var master in ModbusMasterDictionary.Values)
            {
                master.Dispose();
            }

            foreach (var port in SerialPortDictionary.Values)
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
            }
        }
    }

    public class IntegerRangeValidationRule : ValidationRule
    {
        public int Min { get; set; }
        public int Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (int.TryParse((string)value, out int result))
            {
                if (result < Min || result > Max)
                {
                    return new ValidationResult(false, $"请输入一个介于 {Min} 和 {Max} 之间的整数。");
                }
                return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, "请输入一个有效的整数。");
        }
    }
}
