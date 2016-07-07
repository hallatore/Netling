using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Netling.Core;
using Netling.Core.Models;

namespace Netling.Client
{
    public partial class MainWindow
    {
        private bool _running;
        private CancellationTokenSource _cancellationTokenSource;
        private Task<JobResult> _task;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Urls.Focus();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_running)
            {
                var duration = default(TimeSpan);
                var threads = Convert.ToInt32(Threads.SelectionBoxItem);
                var pipelining = Convert.ToInt32(Pipelining.SelectionBoxItem);
                var durationText = (string)((ComboBoxItem)Duration.SelectedItem).Content;
                StatusProgressbar.IsIndeterminate = false;

                switch (durationText)
                {
                    case "10 seconds":
                        duration = TimeSpan.FromSeconds(10);
                        break;
                    case "20 seconds":
                        duration = TimeSpan.FromSeconds(20);
                        break;
                    case "1 minute":
                        duration = TimeSpan.FromMinutes(1);
                        break;
                    case "10 minutes":
                        duration = TimeSpan.FromMinutes(10);
                        break;
                    case "1 hour":
                        duration = TimeSpan.FromHours(1);
                        break;
                    case "Until canceled":
                        duration = TimeSpan.MaxValue;
                        StatusProgressbar.IsIndeterminate = true;
                        break;

                }

                if (string.IsNullOrWhiteSpace(Urls.Text))
                    return;

                var url = Urls.Text.Trim();

                Threads.IsEnabled = false;
                Duration.IsEnabled = false;
                Urls.IsEnabled = false;

                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;
                var job = new PerformanceJob();

                StatusProgressbar.Value = 0;
                StatusProgressbar.Visibility = Visibility.Visible;
                
                _task = Task.Factory.StartNew(() => job.Process(threads, pipelining, duration, url, cancellationToken), TaskCreationOptions.LongRunning);
                _task.GetAwaiter().OnCompleted(JobCompleted);

                StartButton.Content = "Cancel";
                _running = true;

                if (StatusProgressbar.IsIndeterminate)
                    return;

                var sw = new Stopwatch();
                sw.Start();

                while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
                {
                    await Task.Delay(10);
                    StatusProgressbar.Value = 100.0 / duration.TotalMilliseconds * sw.Elapsed.TotalMilliseconds;
                }
            }
            else
            {
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();
            }
        }

        private void Urls_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            StartButton_Click(sender, null);
            StartButton.Focus();
        }

        private void OnProgress(double amount)
        {
            Dispatcher.InvokeAsync(() => StatusProgressbar.Value = amount, DispatcherPriority.Background);
        }

        private void JobCompleted()
        {
            Threads.IsEnabled = true;
            Duration.IsEnabled = true;
            Urls.IsEnabled = true;
            StartButton.Content = "Run";
            StatusProgressbar.Visibility = Visibility.Hidden;
            _cancellationTokenSource = null;
            _running = false;

            var result = new ResultWindow(_task.Result);
            _task = null;
            result.Show();
        }
    }
}
