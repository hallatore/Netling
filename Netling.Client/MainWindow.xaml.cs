using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
                var threads = Convert.ToInt32(Threads.SelectionBoxItem);
                var runs = Convert.ToInt32(Runs.Text);
                var urls = Regex.Split(Urls.Text, "\r\n").Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim());

                if (!urls.Any())
                    return;

                cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = cancellationTokenSource.Token;
                var job = new Job<UrlResult>();

                StatusProgressbar.Maximum = threads * runs * urls.Count();
                StatusProgressbar.Visibility = Visibility.Visible;
                job.OnProgress += OnProgress;

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

        private async void OnProgress(int amount)
        {
            await Dispatcher.InvokeAsync(() => StatusProgressbar.Value = amount, DispatcherPriority.Background);
        }

        private void JobCompleted()
        {
            StartButton.Content = "Run";
            StatusProgressbar.Visibility = Visibility.Hidden;
            StatusProgressbar.Value = 0;
            cancellationTokenSource = null;
            running = false;

            var result = new ResultWindow(task.Result);
            task = null;
            result.Show();
        }
    }
}
