using System;
using System.Collections.Generic;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;
using NModbus.IO;
using ModbusWPF.Models;
using NModbus.Device;
using NModbus.Extensions.Enron;
using System.Diagnostics;
using System.Windows;

namespace ModbusWPF.Helper
{
    public class ModBusHelper
    {
        private readonly Dictionary<string, SerialPort> _serialPortDictionary = new();
        private readonly Dictionary<string, IModbusMaster> _modbusMasterDictionary = new();

        public ModBusHelper()
        {
            InitializeSerialPorts();
        }

        private void InitializeSerialPorts()
        {
            var port8 = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 1000, // 读操作超时时间（毫秒）
                WriteTimeout = 1000 // 写操作超时时间（毫秒）
            }; 

            _serialPortDictionary["COM3"] = port8;

            foreach (var port in _serialPortDictionary.Values)
            {
                if (!port.IsOpen)
                {
                    port.Open();
                }
            }

            var factory = new ModbusFactory();
            _modbusMasterDictionary["COM3"] = factory.CreateRtuMaster(new SerialPortAdapter(_serialPortDictionary["COM3"]));
        }

        //向从机发送读取请求并自动更新DataPoint
        public void ReadBoolData(BoolDataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            bool coilStatus = master.ReadCoilsAsync(slaveAddress, registerAddress, 1).Result[0];
            dataPoint.Value = coilStatus;
            Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteBoolData(BoolDataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            master.WriteSingleCoilAsync(slaveAddress, registerAddress, dataPoint.Value);
        }

        public void ReadInt16Data(Int16DataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort intValue = master.ReadHoldingRegistersAsync(slaveAddress, registerAddress, 1).Result[0];
            dataPoint.Value = (short)intValue;
            Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteInt16Data(Int16DataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            master.WriteSingleRegisterAsync(slaveAddress, registerAddress, (ushort)dataPoint.Value);
        }

        public void ReadFloat32Data(Float32DataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort[] registersFloat32 = master.ReadHoldingRegistersAsync(slaveAddress, registerAddress, 2).Result;
            float floatValue = BitConverter.ToSingle(BitConverter.GetBytes(registersFloat32[0] << 16 | registersFloat32[1]), 0); ;
            dataPoint.Value = floatValue;
            Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteFloat32Data(Float32DataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;
            var bytes = BitConverter.GetBytes(dataPoint.Value);
            ushort[] data = { BitConverter.ToUInt16(bytes, 0), BitConverter.ToUInt16(bytes, 2) };
            master.WriteMultipleRegistersAsync(slaveAddress, registerAddress, data);
        }

        public void Close()
        {
            foreach (var master in _modbusMasterDictionary.Values)
            {
                master.Dispose();
            }

            foreach (var port in _serialPortDictionary.Values)
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
            }
        }
    }
}
