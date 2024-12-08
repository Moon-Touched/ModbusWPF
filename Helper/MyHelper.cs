using System;
using System.Collections.Generic;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;
using NModbus.IO;
using ModbusWPF.Models;
using NModbus.Device;

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
            var port2 = new SerialPort("COM2", 9600, Parity.None, 8, StopBits.One);

            _serialPortDictionary["COM2"] = port2;

            foreach (var port in _serialPortDictionary.Values)
            {
                if (!port.IsOpen)
                {
                    port.Open();
                }
            }

            var factory = new ModbusFactory();
            _modbusMasterDictionary["COM2"] = factory.CreateRtuMaster(new SerialPortAdapter(_serialPortDictionary["COM2"]));
        }

        //向从机发送读取请求并自动更新DataPoint
        public void ReadBoolData(BoolDataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            bool coilStatus = master.ReadCoils(slaveAddress, registerAddress, 1)[0];
            dataPoint.Value = coilStatus;
        }

        public void WriteBoolData(BoolDataPoint dataPoint, bool value)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            master.WriteSingleCoil(slaveAddress, registerAddress, value);
        }

        public void ReadInt16Data(Int16DataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort intValue = master.ReadHoldingRegisters(slaveAddress, registerAddress, 1)[0];
            dataPoint.Value = (short)intValue;
        }

        public void WriteInt16Data(Int16DataPoint dataPoint, ushort value)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            master.WriteSingleRegister(slaveAddress, registerAddress, value);
        }

        public void ReadFloat32Data(Float32DataPoint dataPoint)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;

            ushort[] registersFloat32 = master.ReadHoldingRegisters(slaveAddress, registerAddress, 2);
            float floatValue = BitConverter.ToSingle(BitConverter.GetBytes(registersFloat32[0] << 16 | registersFloat32[1]), 0); ;
            dataPoint.Value = floatValue;
        }

        public void WriteFloat32Data(Float32DataPoint dataPoint, float value)
        {
            var master = _modbusMasterDictionary[dataPoint.PortName];
            byte slaveAddress = (byte)dataPoint.SlaveAddress;
            ushort registerAddress = (ushort)dataPoint.RegisterAddress;
            var bytes = BitConverter.GetBytes(value);
            ushort[] data = { BitConverter.ToUInt16(bytes, 0), BitConverter.ToUInt16(bytes, 2) };
            master.WriteMultipleRegisters(slaveAddress, registerAddress, data);
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
