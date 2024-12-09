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
                string portNamm= info[0];
                int baudRate = int.Parse(info[1]);
                int parity = int.Parse(info[4]);
                int dataBits = int.Parse(info[2]);
                int stopBits = int.Parse(info[3]);

                var port = new SerialPort(portNamm, baudRate, (Parity)parity, dataBits, (StopBits)stopBits);

                SerialPortDictionary[portNamm] = port;

                if (!port.IsOpen)
                {
                    port.Open();
                }

                var factory = new ModbusFactory();
                ModbusMasterDictionary[portNamm] = factory.CreateRtuMaster(new SerialPortAdapter(port));
            }
        }

        //向从机发送读取请求并自动更新DataPoint
        public void ReadBoolData(BoolDataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            bool coilStatus = master.ReadCoilsAsync(slaveAddress, registerAddress, 1).Result[0];
            dataPoint.Value = coilStatus;
            Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteBoolData(BoolDataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            master.WriteSingleCoilAsync(slaveAddress, registerAddress, dataPoint.Value);
        }

        public void ReadInt16Data(Int16DataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort intValue = master.ReadHoldingRegistersAsync(slaveAddress, registerAddress, 1).Result[0];
            dataPoint.Value = (short)intValue;
            Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteInt16Data(Int16DataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            master.WriteSingleRegisterAsync(slaveAddress, registerAddress, (ushort)dataPoint.Value);
        }

        public void ReadFloat32Data(Float32DataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort[] registersFloat32 = master.ReadHoldingRegistersAsync(slaveAddress, registerAddress, 2).Result;
            float floatValue = BitConverter.ToSingle(BitConverter.GetBytes(registersFloat32[0] << 16 | registersFloat32[1]), 0); ;
            dataPoint.Value = floatValue;
            Debug.WriteLine(dataPoint.Name + ":" + dataPoint.Value);
        }

        public void WriteFloat32Data(Float32DataPoint dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;
            var bytes = BitConverter.GetBytes(dataPoint.Value);
            ushort[] data = { BitConverter.ToUInt16(bytes, 0), BitConverter.ToUInt16(bytes, 2) };
            master.WriteMultipleRegistersAsync(slaveAddress, registerAddress, data);
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
}
