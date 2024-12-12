# 简介
用于Modbus通信的界面开发，可实现int16，float32，bool类型的数据读写。可以定期记录csv格式数据日志
使用的第三方库：NModbus，MaterialDesignWPFToolKit
[]数据曲线待完善

# Model

用于表示不同类型数据点的类。基类 `DataPointBase` 实现 `INotifyPropertyChanged` 接口，并声明从机地址，寄存器地址，数据类型标记等信息。

派生类添加不同类型的`Value`属性，对应数据类型标记。

* DataPointBase

  * `string Name`：数据点的名称。
  * `string PortName`：端口名称。
  * `string DataType`：数据类型。
  * `int SlaveAddress`：从站地址。
  * `int RegisterAddress`：寄存器地址。
  * `bool ReadOnly`：是否只读。
* `BoolDataPoint`
* `Int16DataPoint`  16 位整数(short)类型。
* `Float32DataPoint`  32 位浮点数(float)类型。
* `FloatIntDataPoint` 存储的是浮点数据，但是从机使用乘以10后的整数表示，额外添加转换。
* `BoolIntDataPoint` 存储的是bool数据，但是从机使用整数表示，额外添加转换。

# ViewModel

* DataPointViewModel：管理数据点的加载，读写，记录。

  * 属性

    * `ModBusHelper ModbusHelper`：Modbus 通信的工具类。
    * `Dictionary<string, DataPointBase> DataPointsDictionary`：存储数据点的字典，键为数据点名称，值为数据点实例。
    * `Stack<(string taskType, string dataName)> taskStack`：读写任务堆栈，`taskType`用"R"或"W"表示读写，从`DataPointsDictionary`中根据 `dataName`获取数据点实例。
  * 构造函数

    * `DataPointViewModel(string dataCSVPath, string portCSVPath)`：初始化数据点和`ModbusHelper`。
  * 方法

    * `private void LoadDataPointsFromCsv(string dataCSVPath)`：在构造函数中调用，根据CSV 文件信息建立不同数据类型的`DataPoint`实例，并读取从机当前值作为初始值。数据点存储在 `DataPointsDictionary` 中。
    * `public async void ProcessTaskQueue(int delayMilliseconds)`：在主窗口加载后调用。每`delayMilliseconds`处理`taskStack`中的一个任务，如果队列为空，就把所有数据的读取任务依次加入。
    * `private void DataPointPropertyChangedHandler(object sender, PropertyChangedEventArgs e)`：用户输入触发`DataPoint`属性更改事件，将写入任务添加到任务堆栈。
    * `public async void RecordData(string dataFolderPath, int sampleTimeMillisecond)`：在主窗口加载后调用，先根据当前日期新建文件、写入表头，之后定期记录日期，时间，数据，到所有数据到 CSV 文件。

# 自定义控件

* 存储的数据是bool类型，则有State属性，绑定示例：

```xmal
<myControl:LabeledToggleSwitch x:Name="Auto_Manual" LabelText="Auto_Manual" HorizontalAlignment="Left" Margin="32,10,0,0" VerticalAlignment="Top"
                       IsHitTestVisible="True"
                       State="{Binding DataPointsDictionary[Auto_Manual].Value, Mode=TwoWay}"/>
```

* 存储的是int16或float32，则有ValueText属性，绑定示例：

```xmal
<myControl:LabeledTextBox x:Name="PV" LabelText="PV" HorizontalAlignment="Left" Margin="36,66,0,0" VerticalAlignment="Top" Width="90"
                      IsHitTestVisible="False"
                      ValueText="{Binding DataPointsDictionary[PV].Value, Mode=TwoWay}"/>

<myControl:LabeledDecimalUpDown x:Name="SV" LabelText="SV" HorizontalAlignment="Left" Margin="36,117,0,0" VerticalAlignment="Top" Width="90"
                      IsHitTestVisible="True"
                      ValueText="{Binding DataPointsDictionary[SV].Value, Mode=TwoWay}"
                      MinValue="0" MaxValue="10"/>
```

# Tips

`TextBox`更改内容后，需要丢失焦点（LostFocus）才触发属性更改，但是只有点击其他控件或按<kbd>Tab</kbd>键才丢失焦点。为`Grid`添加点击事件，点击后清空所有焦点。
