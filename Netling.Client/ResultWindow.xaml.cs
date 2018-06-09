using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Netling.Core.Models;
using OxyPlot;

namespace Netling.Client
{
    public partial class ResultWindow
    {
        private ResultWindowItem _resultWindowItem;
        private readonly MainWindow _sender;

        public ResultWindow(MainWindow sender)
        {
            _sender = sender;
            InitializeComponent();
        }

        public async Task Load(WorkerResult workerResult)
        {
            var taskResult = await GenerateAsync(workerResult);
            _resultWindowItem = taskResult.ResultWindowItem;

            RequestsPerSecondGraph.Draw(taskResult.Throughput, "{Y:#,0} rps");
            var dataPoints = workerResult.Histogram.Select((count, i) => new DataPoint(i / 80.0 * (_resultWindowItem.Max - _resultWindowItem.Min) + _resultWindowItem.Min, count)).ToList();
            HistogramGraph.Draw(dataPoints, "{X:0.000} ms");

            Title = "Netling - " + _resultWindowItem.Url;
            ThreadsValueUserControl.Value = _resultWindowItem.Threads.ToString();

            RequestsValueUserControl.Value = _resultWindowItem.JobsPerSecond.ToString("#,0");
            ElapsedValueUserControl.Value = $"{_resultWindowItem.ElapsedSeconds:0}";
            BandwidthValueUserControl.Value = _resultWindowItem.Bandwidth.ToString("#,0");
            ErrorsValueUserControl.Value = _resultWindowItem.Errors.ToString("#,0");
            MedianValueUserControl.Value = string.Format(_resultWindowItem.Median > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Median);
            StdDevValueUserControl.Value = string.Format(_resultWindowItem.StdDev > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.StdDev);
            MinValueUserControl.Value = string.Format(_resultWindowItem.Min > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Min);
            MaxValueUserControl.Value = string.Format(_resultWindowItem.Max > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Max);
            MinTextBlock.Text = MinValueUserControl.Value + " ms";
            MaxTextBlock.Text = MaxValueUserControl.Value + " ms";

            var errors = new Dictionary<string, string>();

            foreach (var statusCode in workerResult.StatusCodes)
            {
                errors.Add(statusCode.Key.ToString(), statusCode.Value.ToString("#,0"));
            }

            foreach (var exception in workerResult.Exceptions)
            {
                errors.Add(exception.Key.ToString(), exception.Value.ToString("#,0"));
            }

            ErrorsListView.ItemsSource = errors;

            if (_sender.ResultWindowItem != null)
                LoadBaseline(_sender.ResultWindowItem);
        }

        private Task<JobTaskResult> GenerateAsync(WorkerResult workerResult)
        {
            return Task.Run(() =>
            {
                var result = ResultWindowItem.Parse(workerResult);
                var max = (int) Math.Floor(workerResult.Elapsed.TotalMilliseconds / 1000);

                var throughput = workerResult.Seconds
                    .Where(r => r.Key < max && r.Value.Count > 0)
                    .OrderBy(r => r.Key)
                    .Select(r => new DataPoint(r.Key, r.Value.Count));

                return new JobTaskResult
                {
                    ResultWindowItem = result,
                    Throughput = throughput
                };
            });
        }

        private void UseBaseline(object sender, RoutedEventArgs e)
        {
            _sender.ResultWindowItem = _resultWindowItem;
            LoadBaseline(_sender.ResultWindowItem);
        }

        private void ClearBaseline(object sender, RoutedEventArgs e)
        {
            _sender.ResultWindowItem = null;
            ThreadsValueUserControl.BaselineValue = null;

            RequestsValueUserControl.BaselineValue = null;
            RequestsValueUserControl.BaseLine = BaseLine.Equal;

            ElapsedValueUserControl.BaselineValue = null;
            ElapsedValueUserControl.BaseLine = BaseLine.Equal;

            BandwidthValueUserControl.BaselineValue = null;
            BandwidthValueUserControl.BaseLine = BaseLine.Equal;

            ErrorsValueUserControl.BaselineValue = null;
            ErrorsValueUserControl.BaseLine = BaseLine.Equal;

            MedianValueUserControl.BaselineValue = null;
            MedianValueUserControl.BaseLine = BaseLine.Equal;

            StdDevValueUserControl.BaselineValue = null;
            StdDevValueUserControl.BaseLine = BaseLine.Equal;

            MinValueUserControl.BaselineValue = null;
            MinValueUserControl.BaseLine = BaseLine.Equal;

            MaxValueUserControl.BaselineValue = null;
            MaxValueUserControl.BaseLine = BaseLine.Equal;
        }

        private void LoadBaseline(ResultWindowItem baseline)
        {
            ThreadsValueUserControl.BaselineValue = baseline.Threads.ToString();

            RequestsValueUserControl.BaselineValue = $"{baseline.JobsPerSecond:#,0}";
            RequestsValueUserControl.BaseLine = GetBaseline(_resultWindowItem.JobsPerSecond, baseline.JobsPerSecond);

            ElapsedValueUserControl.BaselineValue = $"{baseline.ElapsedSeconds:#,0}";

            BandwidthValueUserControl.BaselineValue = $"{baseline.Bandwidth:0}";
            BandwidthValueUserControl.BaseLine = GetBaseline(_resultWindowItem.Bandwidth, baseline.Bandwidth);

            ErrorsValueUserControl.BaselineValue = baseline.Errors.ToString();
            ErrorsValueUserControl.BaseLine = GetBaseline(baseline.Errors, _resultWindowItem.Errors);

            MedianValueUserControl.BaselineValue = string.Format(baseline.Median > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Median);
            MedianValueUserControl.BaseLine = GetBaseline(baseline.Median, _resultWindowItem.Median);

            StdDevValueUserControl.BaselineValue = string.Format(baseline.StdDev > 5 ? "{0:#,0}" : "{0:0.000}", baseline.StdDev);
            StdDevValueUserControl.BaseLine = GetBaseline(baseline.StdDev, _resultWindowItem.StdDev);

            MinValueUserControl.BaselineValue = string.Format(baseline.Min > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Min);
            MinValueUserControl.BaseLine = GetBaseline(baseline.Min, _resultWindowItem.Min);

            MaxValueUserControl.BaselineValue = string.Format(baseline.Max > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Max);
            MaxValueUserControl.BaseLine = GetBaseline(baseline.Max, _resultWindowItem.Max);
        }

        private BaseLine GetBaseline(double v1, double v2)
        {
            if (Math.Abs(v1 - v2) < 0.001)
                return BaseLine.Equal;

            return v1 > v2 ? BaseLine.Better : BaseLine.Worse;
        }
    }

    internal class JobTaskResult
    {
        public ResultWindowItem ResultWindowItem { get; set; }
        public IEnumerable<DataPoint> Throughput { get; set; }
    }
}
