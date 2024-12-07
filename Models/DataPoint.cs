using System.ComponentModel;

namespace ModbusWPF.Models
{
    public abstract class DataPointBase
    {
        public string Name { get; set; }
        public string PortName { get; set; }
        public string DataType { get; set; }
        public int SlaveAddress { get; set; }
        public int RegisterAddress { get; set; }
        public bool ReadOnly { get; set; }
    }

    public class BoolDataPoint : DataPointBase,INotifyPropertyChanged
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
}