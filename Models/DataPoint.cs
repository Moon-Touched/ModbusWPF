using System.ComponentModel;

namespace ModbusWPF.Models
{
    public abstract class DataPointBase : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string PortName { get; set; }
        public string DataType { get; set; }
        public int SlaveAddress { get; set; }
        public int RegisterAddress { get; set; }
        public bool ReadOnly { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class BoolDataPoint : DataPointBase
    {
        private bool _value;
        public bool Value
        {
            get => _value;
            set
            {
                if (!(_value == value))
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public BoolDataPoint(string name, string dataType, string portName, int slaveAddress, int registerAddress, bool readOnly, bool initialValue)
        {
            Name = name;
            DataType = dataType;
            PortName = portName;
            SlaveAddress = slaveAddress;
            RegisterAddress = registerAddress;
            ReadOnly = readOnly;
            Value = initialValue;
        }
    }
    public class Int16DataPoint : DataPointBase, INotifyPropertyChanged
    {
        private short _value;
        public short Value
        {
            get => _value;
            set
            {
                if (!(_value == value))
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public Int16DataPoint(string name, string dataType, string portName, int slaveAddress, int registerAddress, bool readOnly, short initialValue)
        {
            Name = name;
            DataType = dataType;
            PortName = portName;
            SlaveAddress = slaveAddress;
            RegisterAddress = registerAddress;
            ReadOnly = readOnly;
            Value = initialValue;
        }
    }
    public class Float32DataPoint : DataPointBase, INotifyPropertyChanged
    {
        private float _value;
        public float Value
        {
            get => _value;
            set
            {
                if (!(_value == value))
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
        public Float32DataPoint(string name, string dataType, string portName, int slaveAddress, int registerAddress, bool readOnly, float initialValue)
        {
            Name = name;
            DataType = dataType;
            PortName = portName;
            SlaveAddress = slaveAddress;
            RegisterAddress = registerAddress;
            ReadOnly = readOnly;
            Value = initialValue;
        }
    }

    public class FloatIntDataPoint : DataPointBase, INotifyPropertyChanged
    {
        private float _value;
        public float Value
        {
            get => _value;
            set
            {
                if (!(_value == value))
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
        public FloatIntDataPoint(string name, string dataType, string portName, int slaveAddress, int registerAddress, bool readOnly, float initialValue)
        {
            Name = name;
            DataType = dataType;
            PortName = portName;
            SlaveAddress = slaveAddress;
            RegisterAddress = registerAddress;
            ReadOnly = readOnly;
            Value = initialValue;
        }
    }

    public class BoolIntDataPoint : DataPointBase, INotifyPropertyChanged
    {
        private bool _value;
        public bool Value
        {
            get => _value;
            set
            {
                if (!(_value == value))
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public BoolIntDataPoint(string name, string dataType, string portName, int slaveAddress, int registerAddress, bool readOnly, bool initialValue)
        {
            Name = name;
            DataType = dataType;
            PortName = portName;
            SlaveAddress = slaveAddress;
            RegisterAddress = registerAddress;
            ReadOnly = readOnly;
            Value = initialValue;
        }
    }
}