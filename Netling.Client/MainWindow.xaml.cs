using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Netling.Core;
using Netling.Core.Models;

namespace Netling.Client
{
    public partial class MainWindow : Window
    {
        private bool running = false;
        private CancellationTokenSource cancellationTokenSource;
        private Task<JobResult<UrlResult>> task;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!running)
            {
                var timeLimited = false;
                TimeSpan duration = default(TimeSpan);
                int runs = 0;
                var threads = Convert.ToInt32(Threads.SelectionBoxItem);
                var runsText = (string)((ComboBoxItem)Runs.SelectedItem).Content;

                switch (runsText)
                {
                    case "10":
                        runs = 10;
                        break;
                    case "100":
                        runs = 100;
                        break;
                    case "10 seconds":
                        duration = TimeSpan.FromSeconds(10);
                        timeLimited = true;
                        break;
                    case "20 seconds":
                        duration = TimeSpan.FromSeconds(20);
                        timeLimited = true;
                        break;
                    case "1 minute":
                        duration = TimeSpan.FromMinutes(1);
                        timeLimited = true;
                        break;
                    case "10 minutes":
                        duration = TimeSpan.FromMinutes(10);
                        timeLimited = true;
                        break;
                    case "1 hour":
                        duration = TimeSpan.FromHours(1);
                        timeLimited = true;
                        break;
                    case "Unlimited":
                        duration = TimeSpan.MaxValue;
                        timeLimited = true;
                        break;

                }

                var urls = Regex.Split(Urls.Text, "\r\n").Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim());

                if (!urls.Any())
                    return;

                cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;
                var job = new Job<UrlResult>();

                StatusProgressbar.Value = 0;
                StatusProgressbar.Visibility = Visibility.Visible;
                job.OnProgress += OnProgress;

                if (timeLimited)
                    task = Task.Run(() => job.ProcessUrls(threads, duration, urls, cancellationToken));
                else
                    task = Task.Run(() => job.ProcessUrls(threads, runs, urls, cancellationToken));


                var awaiter = task.GetAwaiter();
                awaiter.OnCompleted(JobCompleted);

                StartButton.Content = "Cancel";
                running = true;
            }
            else
            {
                if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                    cancellationTokenSource.Cancel();
            }
        }

        private void OnProgress(double amount)
        {
            Dispatcher.InvokeAsync(() => StatusProgressbar.Value = amount, DispatcherPriority.Background);
        }

        private void JobCompleted()
        {
            StartButton.Content = "Run";
            StatusProgressbar.Visibility = Visibility.Hidden;
            cancellationTokenSource = null;
            running = false;

            var result = new ResultWindow(task.Result);
            task = null;
            result.Show();
        }
    }
}
