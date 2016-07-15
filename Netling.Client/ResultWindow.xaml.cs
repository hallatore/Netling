using System;
using System.Collections.Generic;
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

            RequestsPerSecondGraph.Draw(taskResult.Throughput);
            HistogramGraph.Draw(taskResult.Latency);

            Title = "Netling - " + _resultWindowItem.Url;
            ThreadsValueUserControl.Value = _resultWindowItem.Threads.ToString();
            PipeliningValueUserControl.Value = _resultWindowItem.Pipelining.ToString();
            ThreadAfinityValueUserControl.Value = _resultWindowItem.ThreadAfinity ? "ON" : "OFF";

            RequestsValueUserControl.Value = $"{_resultWindowItem.JobsPerSecond:#,0}";
            ElapsedValueUserControl.Value = $"{_resultWindowItem.ElapsedSeconds:#,0}";
            BandwidthValueUserControl.Value = $"{_resultWindowItem.Bandwidth:0}";
            ErrorsValueUserControl.Value = _resultWindowItem.Errors.ToString();
            MedianValueUserControl.Value = string.Format(_resultWindowItem.Median > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Median);
            StdDevValueUserControl.Value = string.Format(_resultWindowItem.StdDev > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.StdDev);
            MinValueUserControl.Value = string.Format(_resultWindowItem.Min > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Min);
            MaxValueUserControl.Value = string.Format(_resultWindowItem.Max > 5 ? "{0:#,0}" : "{0:0.000}", _resultWindowItem.Max);

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

                var latency = GenerateHistogram(workerResult.ResponseTimes);

                return new JobTaskResult
                {
                    ResultWindowItem = result,
                    Throughput = throughput,
                    Latency = latency
                };
            });
        }

        private List<DataPoint> GenerateHistogram(double[] responeTimes)
        {
            var result = new List<DataPoint>();

            if (responeTimes == null || responeTimes.Length < 2)
                return result;

            var max = responeTimes.Last();
            var min = responeTimes.First();

            var splits = 200;
            var divider = (max - min) / splits;
            var step = min;
            var y = 0;

            for (var i = 0; i < splits; i++)
            {
                var count = 0;
                var stepMax = step + divider;

                if (i + 1 == splits)
                    stepMax = double.MaxValue;

                while (y < responeTimes.Length && responeTimes[y] < stepMax)
                {
                    y++;
                    count++;
                }

                result.Add(new DataPoint(step + (divider / 2), count));
                step += divider;
            }

            return result;
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
            PipeliningValueUserControl.BaselineValue = null;
            ThreadAfinityValueUserControl.BaselineValue = null;

            RequestsValueUserControl.BaselineValue = null;
            RequestsValueUserControl.ValueBrush = null;

            ElapsedValueUserControl.BaselineValue = null;
            ElapsedValueUserControl.ValueBrush = null;

            BandwidthValueUserControl.BaselineValue = null;
            BandwidthValueUserControl.ValueBrush = null;

            ErrorsValueUserControl.BaselineValue = null;
            ErrorsValueUserControl.ValueBrush = null;

            MedianValueUserControl.BaselineValue = null;
            MedianValueUserControl.ValueBrush = null;

            StdDevValueUserControl.BaselineValue = null;
            StdDevValueUserControl.ValueBrush = null;

            MinValueUserControl.BaselineValue = null;
            MinValueUserControl.ValueBrush = null;

            MaxValueUserControl.BaselineValue = null;
            MaxValueUserControl.ValueBrush = null;
        }

        private void LoadBaseline(ResultWindowItem baseline)
        {
            ThreadsValueUserControl.BaselineValue = baseline.Threads.ToString();
            PipeliningValueUserControl.BaselineValue = baseline.Pipelining.ToString();
            ThreadAfinityValueUserControl.BaselineValue = baseline.ThreadAfinity ? "ON" : "OFF";

            RequestsValueUserControl.BaselineValue = $"{baseline.JobsPerSecond:#,0}";
            RequestsValueUserControl.ValueBrush = GetValueBrush(_resultWindowItem.JobsPerSecond, baseline.JobsPerSecond);

            ElapsedValueUserControl.BaselineValue = $"{baseline.ElapsedSeconds:#,0}";

            BandwidthValueUserControl.BaselineValue = $"{baseline.Bandwidth:0}";
            BandwidthValueUserControl.ValueBrush = GetValueBrush(_resultWindowItem.Bandwidth, baseline.Bandwidth);

            ErrorsValueUserControl.BaselineValue = baseline.Errors.ToString();
            ErrorsValueUserControl.ValueBrush = GetValueBrush(baseline.Errors, _resultWindowItem.Errors);

            MedianValueUserControl.BaselineValue = string.Format(baseline.Median > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Median);
            MedianValueUserControl.ValueBrush = GetValueBrush(baseline.Median, _resultWindowItem.Median);

            StdDevValueUserControl.BaselineValue = string.Format(baseline.StdDev > 5 ? "{0:#,0}" : "{0:0.000}", baseline.StdDev);
            StdDevValueUserControl.ValueBrush = GetValueBrush(baseline.StdDev, _resultWindowItem.StdDev);

            MinValueUserControl.BaselineValue = string.Format(baseline.Min > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Min);
            MinValueUserControl.ValueBrush = GetValueBrush(baseline.Min, _resultWindowItem.Min);

            MaxValueUserControl.BaselineValue = string.Format(baseline.Max > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Max);
            MaxValueUserControl.ValueBrush = GetValueBrush(baseline.Max, _resultWindowItem.Max);
        }

        private Brush GetValueBrush(double v1, double v2)
        {
            if (Math.Abs(v1 - v2) < 0.001)
                return null;

            return v1 > v2 ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
        }
    }

    internal class JobTaskResult
    {
        public ResultWindowItem ResultWindowItem { get; set; }
        public IEnumerable<DataPoint> Throughput { get; set; }
        public List<DataPoint> Latency { get; set; }
    }
}
