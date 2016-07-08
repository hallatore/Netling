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
            ResponseTime.Text = string.Format(result.AverageResponseTime > 5 ? "{0:#,0}" : "{0:0.00}", result.AverageResponseTime);
            LoadGraphs(result);
        }

        private void LoadGraphs(JobResult result)
        {
            var max = (int)Math.Floor(result.ElapsedMilliseconds / 1000);

            var dataPoints = result.Seconds
                .Where(r => r.Key < max)
                .OrderBy(r => r.Key)
                .Select(r => new DataPoint(r.Key, r.Value.Count));

            RequestsPerSecondGraph.Draw(dataPoints);
        }
    }
}
