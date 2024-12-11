using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;
using NModbus.IO;
using ModbusWPF.Models;
using NModbus.Device;
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
                string portName = info[0];
                int baudRate = int.Parse(info[1]);
                int parity = int.Parse(info[4]);
                int dataBits = int.Parse(info[2]);
                int stopBits = int.Parse(info[3]);

                var port = new SerialPort(portName, baudRate, (Parity)parity, dataBits, (StopBits)stopBits)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

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

        public void ReadData(DataPointBase dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            switch (dataPoint)
            {
                case BoolDataPoint boolDataPoint:
                    boolDataPoint.Value = master.ReadCoils(slaveAddress, registerAddress, 1)[0];
                    break;
                case Int16DataPoint int16DataPoint:
                    int16DataPoint.Value = (short)master.ReadHoldingRegisters(slaveAddress, registerAddress, 1)[0];
                    break;
                case Float32DataPoint float32DataPoint:
                    ushort[] registersFloat32 = master.ReadHoldingRegisters(slaveAddress, registerAddress, 2);
                    float32DataPoint.Value = BitConverter.ToSingle(BitConverter.GetBytes(registersFloat32[0] << 16 | registersFloat32[1]), 0);
                    break;
                case FloatIntDataPoint floatIntDataPoint:
                    floatIntDataPoint.Value = master.ReadHoldingRegisters(slaveAddress, registerAddress, 1)[0] / 10f;
                    break;
                case BoolIntDataPoint boolIntDataPoint:
                    boolIntDataPoint.Value = master.ReadHoldingRegisters(slaveAddress, registerAddress, 1)[0] != 0;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported data type: {dataPoint.DataType}");
            }
        }

        public void WriteData(DataPointBase dataPoint)
        {
            var master = ModbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            switch (dataPoint)
            {
                case BoolDataPoint boolDataPoint:
                    master.WriteSingleCoil(slaveAddress, registerAddress, boolDataPoint.Value);
                    break;
                case Int16DataPoint int16DataPoint:
                    master.WriteSingleRegister(slaveAddress, registerAddress, (ushort)int16DataPoint.Value);
                    break;
                case Float32DataPoint float32DataPoint:
                    var bytes = BitConverter.GetBytes(float32DataPoint.Value);
                    ushort[] data = { BitConverter.ToUInt16(bytes, 2), BitConverter.ToUInt16(bytes, 0) };
                    master.WriteMultipleRegisters(slaveAddress, registerAddress, data);
                    break;
                case FloatIntDataPoint floatIntDataPoint:
                    master.WriteSingleRegister(slaveAddress, registerAddress, (ushort)(floatIntDataPoint.Value * 10));
                    break;
                case BoolIntDataPoint boolIntDataPoint:
                    master.WriteSingleRegister(slaveAddress, registerAddress, boolIntDataPoint.Value ? (ushort)1 : (ushort)0);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported data type: {dataPoint.DataType}");
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
}
