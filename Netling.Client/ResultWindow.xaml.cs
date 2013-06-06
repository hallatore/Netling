using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Netling.Core.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Netling.Client
{
    public partial class ResultWindow : Window
    {
        public JobResult<UrlResult> Result { get; private set; }

        public ResultWindow(JobResult<UrlResult> result)
        {
            InitializeComponent();
            Result = result;

            TotalRequests.Text = result.Count.ToString(CultureInfo.InvariantCulture);
            RequestsPerSecond.Text = string.Format("{0:0}", result.JobsPerSecond);
            ResponseTime.Text = string.Format("{0:0}", result.Results.Where(r => !r.IsError).DefaultIfEmpty(new UrlResult(0, 0, DateTime.Now, null, 0)).Average(r => r.ResponseTime));
            Elapsed.Text = string.Format("{0:0}", result.ElapsedMilliseconds);
            Bandwidth.Text = string.Format("{0:0}", Math.Round(result.BytesPrSecond * 8 / 1024 / 1024, MidpointRounding.AwayFromZero));
            Errors.Text = result.Errors.ToString(CultureInfo.InvariantCulture);

            Title = string.Format("Threads: {0}, runs: {1}, url's: {2}", result.Threads, result.Runs, result.Results.Select(r => r.Url).Distinct().Count());

            LoadUrlSummary();
            LoadGraph();
        }

        private void LoadGraph()
        {
            var result = Result.Results
                .Where(r => !r.IsError)
                .GroupBy(r => (r.StartTime.Ticks/10000 + r.ResponseTime) / 1000)
                .OrderBy(r => r.Key)
                .ToList();

            var plotModel = new PlotModel
                {
                    PlotMargins = new OxyThickness(0)
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
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot,
                    IsZoomEnabled = false,
                    IsPanEnabled = false
                });

            var ls = new LineSeries(OxyColor.Parse("#ff0079c5"));

            foreach (var item in result)
            {
                ls.Points.Add(new DataPoint(item.Key, item.Count()));
            }

            plotModel.Series.Add(ls);
            RequestsPerSecondGraph.Model = plotModel;
        }

        private void LoadUrlSummary()
        {
            var list = new List<SummaryResult>();
            foreach (var url in Result.Results.Select(r => r.Url).Distinct())
            {
                var urlResult = Result.Results.Where(r => r.Url == url).ToList();
                var responseTime = urlResult.Where(r => !r.IsError).DefaultIfEmpty(new UrlResult(0, 0, DateTime.Now, null, 0)).Average(r => r.ResponseTime);

                list.Add(new SummaryResult
                    {
                        Url = url,
                        ResponseTime = (int)responseTime,
                        Errors = urlResult.Count(r => r.IsError),
                        Size = string.Format("{0:0.0}", urlResult.Where(r => !r.IsError).DefaultIfEmpty(new UrlResult(0, 0, DateTime.Now, null, 0)).Average(r => r.Bytes) / 1024)
                    });
            }

            UrlSummary.ItemsSource = list;
        }
    }

    public class SummaryResult
    {
        public string Url { get; set; }
        public string Size { get; set; }
        public int ResponseTime { get; set; }
        public int Errors { get; set; }
    }
}
