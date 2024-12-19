using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace ModbusWPF.ViewModel
{
    public class RealTimePlotViewModel
    {
        public PlotModel PlotModel { get; set; }
        public Dictionary<string, LineSeries> LineSeriesDictionary { get; set; }
        public RealTimePlotViewModel()
        {
            PlotModel = new PlotModel();
            PlotModel.Legends.Add(new Legend { LegendPosition = LegendPosition.RightTop, LegendPlacement = LegendPlacement.Inside });
            PlotModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd\nHH:mm:ss",
            });

            LineSeriesDictionary = new Dictionary<string, LineSeries>();
        }
    }
}
