using System;
using System.Collections.Generic;
using System.IO;
using ModbusWPF.Models;

namespace ModbusWPF.Helper
{
    public static class MyHelper
    {
        public static List<DataPoint> ReadDataPointCSV(string filePath)
        {
            var dataPointList = new List<DataPoint>();
            using (var reader = new StreamReader(filePath))
            {
                string[] headers = null;
                if (!reader.EndOfStream)
                {
                    headers = reader.ReadLine().Split(',');
                }

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Split(',');
                    string name = line[0];
                    string dataType = line[1];
                    string portName = line[2];
                    int slaveAddress = int.Parse(line[3]);
                    int registerAddress = int.Parse(line[4]);
                    bool readOnly = bool.Parse(line[5]);

                    DataPoint dataPoint = new DataPoint(name, dataType, portName, slaveAddress, registerAddress, readOnly);
                    dataPointList.Add(dataPoint);
                }
            }
            return dataPointList;
        }
    }
}