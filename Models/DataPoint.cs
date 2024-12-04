using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;

namespace ModbusWPF.Models
{
    public class DataPoint(string name, string dataType, string portName, int slaveAddress, int registerAddress, bool readOnly)
    {
        public string Name { get; set; } = name;
        public string PortName { get; set; } = portName;
        public string DataType { get; set; } = dataType;
        public int SlaveAddress { get; set; } = slaveAddress;
        public int RegisterAddress { get; set; } = registerAddress;
        public bool ReadOnly { get; set; } = readOnly;
        public object? Control { get; set; } = null;
        public object? Value { get; set; } = null;
    }
}

