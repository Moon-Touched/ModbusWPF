using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Linq;

namespace ModbusWPF.Models
{
    public class DataPoint : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string PortName { get; set; }
        public string DataType { get; set; }
        public int SlaveAddress { get; set; }
        public int RegisterAddress { get; set; }
        public bool ReadOnly { get; set; }
        public object? Control { get; set; }

        private object? _value;
        public object? Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DataPoint(string name, string dataType, string portName, int slaveAddress, int registerAddress, bool readOnly)
        {
            Name = name;
            DataType = dataType;
            PortName = portName;
            SlaveAddress = slaveAddress;
            RegisterAddress = registerAddress;
            ReadOnly = readOnly;
        }
    }
}

