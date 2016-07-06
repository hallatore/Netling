using System;
using System.Linq;
using System.Windows;
using Netling.Core.Models;
using OxyPlot;

namespace Netling.Client
{
    public partial class ResultWindow : Window
    {
        public ResultWindow(JobResult result)
        {
            InitializeComponent();
            TotalRequests.Text = $"{result.Count:#,0}";
            RequestsPerSecond.Text = $"{result.JobsPerSecond:#,0}";
            Elapsed.Text = $"{result.ElapsedMilliseconds:#,0}";
            Bandwidth.Text = $"{Math.Round(result.BytesPrSecond * 8 / 1024 / 1024, MidpointRounding.AwayFromZero):0}";
            Errors.Text = $"{result.Errors:#,0}";
            var avgResponseTime = result.Results.Where(r => !r.IsError).DefaultIfEmpty(new UrlResult(0)).Average(r => r.ResponseTime);
            ResponseTime.Text = string.Format(avgResponseTime > 5 ? "{0:#,0}" : "{0:0.00}", avgResponseTime);
            LoadGraphs(result);
        }

        private void LoadGraphs(JobResult result)
        {
            var max = (int)Math.Floor(result.ElapsedMilliseconds / 1000);

            var dataPoints = result.Results
                .Where(r => !r.IsError)
                .GroupBy(r => (int)((r.Elapsed + r.ResponseTime) / 1000))
                .Where(r => r.Key < max)
                .OrderBy(r => r.Key)
                .Select(r => new DataPoint(r.Key, r.Count()));

            RequestsPerSecondGraph.Draw(dataPoints);
        }
    }
}
