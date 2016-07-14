using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Netling.Core.Models;
using OxyPlot;

namespace Netling.Client
{
    public partial class ResultWindow
    {
        private readonly BaselineResult _result;
        private readonly MainWindow _sender;

        public ResultWindow(JobResult jobResult, MainWindow sender)
        {
            _sender = sender;
            InitializeComponent();

            _result = BaselineResult.Parse(jobResult);
            LoadGraphs(jobResult);

            Title = "Netling - " + _result.Url;
            ThreadsValueUserControl.Value = _result.Threads.ToString();
            PipeliningValueUserControl.Value = _result.Pipelining.ToString();
            ThreadAfinityValueUserControl.Value = _result.ThreadAfinity ? "ON" : "OFF";

            RequestsValueUserControl.Value = $"{_result.JobsPerSecond:#,0}";
            ElapsedValueUserControl.Value = $"{_result.ElapsedSeconds:#,0}";
            BandwidthValueUserControl.Value = $"{_result.Bandwidth:0}";
            ErrorsValueUserControl.Value = _result.Errors.ToString();
            MedianValueUserControl.Value = string.Format(_result.Median > 5 ? "{0:#,0}" : "{0:0.000}", _result.Median);
            StdDevValueUserControl.Value = string.Format(_result.StdDev > 5 ? "{0:#,0}" : "{0:0.000}", _result.StdDev);
            MinValueUserControl.Value = string.Format(_result.Min > 5 ? "{0:#,0}" : "{0:0.000}", _result.Min);
            MaxValueUserControl.Value = string.Format(_result.Max > 5 ? "{0:#,0}" : "{0:0.000}", _result.Max);

            if (_sender.BaselineResult != null)
                LoadBaseline(_sender.BaselineResult);
        }

        private void LoadGraphs(JobResult result)
        {
            var max = (int)Math.Floor(result.ElapsedMilliseconds / 1000);

            var dataPoints = result.Seconds
                .Where(r => r.Key < max && r.Value.Count > 0)
                .OrderBy(r => r.Key)
                .Select(r => new DataPoint(r.Key, r.Value.Count));

            RequestsPerSecondGraph.Draw(dataPoints);

            var h = GenerateHistogram(result.ResponseTimes);
            HistogramGraph.Draw(h);
        }

        private List<DataPoint> GenerateHistogram(List<double> responeTimes)
        {
            var result = new List<DataPoint>();

            if (responeTimes == null || responeTimes.Count < 2)
                return result;

            var input = responeTimes.OrderBy(r => r).ToList();
            var max = input.Last();
            var min = input.First();

            var splits = 200;
            var divider = (max - min) / splits;
            var step = min;

            for (var i = 0; i < splits; i++)
            {
                result.Add(new DataPoint(step + (divider / 2), input.Count(r => r >= step && r < step + divider)));
                step += divider;
            }

            return result;
        }

        private void UseBaseline(object sender, RoutedEventArgs e)
        {
            _sender.BaselineResult = _result;
            LoadBaseline(_sender.BaselineResult);
        }

        private void ClearBaseline(object sender, RoutedEventArgs e)
        {
            _sender.BaselineResult = null;
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

        private void LoadBaseline(BaselineResult baseline)
        {
            ThreadsValueUserControl.BaselineValue = baseline.Threads.ToString();
            PipeliningValueUserControl.BaselineValue = baseline.Pipelining.ToString();
            ThreadAfinityValueUserControl.BaselineValue = baseline.ThreadAfinity ? "ON" : "OFF";

            RequestsValueUserControl.BaselineValue = $"{baseline.JobsPerSecond:#,0}";
            RequestsValueUserControl.ValueBrush = GetValueBrush(_result.JobsPerSecond, baseline.JobsPerSecond);

            ElapsedValueUserControl.BaselineValue = $"{baseline.ElapsedSeconds:#,0}";

            BandwidthValueUserControl.BaselineValue = $"{baseline.Bandwidth:0}";
            BandwidthValueUserControl.ValueBrush = GetValueBrush(_result.Bandwidth, baseline.Bandwidth);

            ErrorsValueUserControl.BaselineValue = baseline.Errors.ToString();
            ErrorsValueUserControl.ValueBrush = GetValueBrush(baseline.Errors, _result.Errors);

            MedianValueUserControl.BaselineValue = string.Format(baseline.Median > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Median);
            MedianValueUserControl.ValueBrush = GetValueBrush(baseline.Median, _result.Median);

            StdDevValueUserControl.BaselineValue = string.Format(baseline.StdDev > 5 ? "{0:#,0}" : "{0:0.000}", baseline.StdDev);
            StdDevValueUserControl.ValueBrush = GetValueBrush(baseline.StdDev, _result.StdDev);

            MinValueUserControl.BaselineValue = string.Format(baseline.Min > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Min);
            MinValueUserControl.ValueBrush = GetValueBrush(baseline.Min, _result.Min);

            MaxValueUserControl.BaselineValue = string.Format(baseline.Max > 5 ? "{0:#,0}" : "{0:0.000}", baseline.Max);
            MaxValueUserControl.ValueBrush = GetValueBrush(baseline.Max, _result.Max);
        }

        private Brush GetValueBrush(double v1, double v2)
        {
            if (Math.Abs(v1 - v2) < 0.001)
                return null;

            return v1 > v2 ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
        }
    }
}
