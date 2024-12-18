# 简介
- 用于Modbus通信的界面开发，可实现int16，float32，bool类型的数据读写。
- 按一定间隔记录csv格式数据日志。
- 历史趋势曲线，可以筛选某一时间段内的数据。
使用的第三方库：NModbus，MahApps.Metro，Oxyplot


# 使用方法：

主界面中放置了示例控件，对应data_points.csv中的数据名字，自己复制添加。写好data_points.csv和port_info.csv，在程序中写好路径运行即可。


- data_points.csv：需要修改路径，格式如下：
表头（第一行）：变量名,数据类型,串口名,从机地址,寄存器起始地址,是否只读
数据（其他行）：Auto_Manual,bool_int,COM2,1,4308,N

- port_info.csv路径：
表头（第一行）：串口名,波特率,数据位,停止位,校验位
数据（其他行）：COM3,9600,8,1,0

- 每次通信的间隔，毫秒，在MainWindow.xaml.cs
  Task.Run(() => dataPointViewModel.ProcessTaskQueue(100));
- 数据记录间隔，毫秒，在MainWindow.xaml.cs
  Task.Run(() => dataPointViewModel.RecordData(HisCSVPath, 1000));
