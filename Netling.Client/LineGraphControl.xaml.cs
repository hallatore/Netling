using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Netling.Client
{
    /// <summary>
    /// Interaction logic for LineGraphControl.xaml
    /// </summary>
    public partial class LineGraphControl : UserControl
    {
        public LineGraphControl()
        {
            InitializeComponent();
        }

        public void Plot(string title, IEnumerable<DataPoint> points)
        {
            Title.Text = title;

            var plotModel = new PlotModel
            {
                PlotMargins = new OxyThickness(0),
                AutoAdjustPlotMargins = false
            };

            plotModel.Axes.Add(new LinearAxis
            {
                MinimumPadding = 0.0,
                MaximumPadding = 0.0,
                IsAxisVisible = false,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                Position = AxisPosition.Bottom
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Minimum = 0.0,
                MaximumPadding = 0.1,
                TickStyle = TickStyle.None,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = false,
                IsPanEnabled = false
            });

            var ls = new LineSeries(OxyColor.Parse("#ff0079c5"));

            foreach (var point in points)
            {
                ls.Points.Add(point);
            }

            plotModel.Series.Add(ls);
            Graph.Model = plotModel;
        }
    }
}
