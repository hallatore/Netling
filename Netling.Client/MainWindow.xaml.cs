using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Netling.Core;
using Netling.Core.HttpClientWorker;
using Netling.Core.Models;
using Netling.Core.SocketWorker;

namespace Netling.Client
{
    public partial class MainWindow
    {
        private bool _running;
        private CancellationTokenSource _cancellationTokenSource;
        private Task<WorkerResult> _task;
        private List<ResultWindow> _resultWindows;
        private ResultWindowItem _baselineResult;

        public MainWindow()
        {
            _resultWindows = new List<ResultWindow>();
            InitializeComponent();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture.NumberFormat.NumberGroupSeparator = " ";
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Threads.SelectedValuePath = "Key";
            Threads.DisplayMemberPath = "Value";

            for (var i = 1; i <= Environment.ProcessorCount; i++)
            {
                Threads.Items.Add(new KeyValuePair<int, string>(i, i.ToString()));
            }

            for (var i = 2; i <= 20; i++)
            {
                Threads.Items.Add(new KeyValuePair<int, string>(Environment.ProcessorCount * i, $"{Environment.ProcessorCount * i} - ({i} per core)"));
            }

            Threads.SelectedIndex = 0;
            Url.Focus();

            List<PluginInfo> pluginList = PluginLoader.GetPluginList(".");
            Plugins.ItemsSource = pluginList.Select(x => string.Format("{0} ({1})", x.TypeName, x.AssemblyShort));
            Plugins.Tag = pluginList;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_running)
            {
                var duration = default(TimeSpan);
                var warmup = TimeSpan.FromSeconds(0);
                int? count = null;
                var threads = Convert.ToInt32(((KeyValuePair<int, string>)Threads.SelectionBoxItem).Key);
                var durationText = (string)((ComboBoxItem)Duration.SelectedItem).Content;
                var warmupText = (string)((ComboBoxItem)Warmup.SelectedItem).Content;
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
                    case "1 run on 1 thread":
                        count = 1;
                        StatusProgressbar.IsIndeterminate = true;
                        break;
                    case "100 runs on 1 thread":
                        count = 100;
                        StatusProgressbar.IsIndeterminate = true;
                        break;
                    case "1000 runs on 1 thread":
                        count = 1000;
                        StatusProgressbar.IsIndeterminate = true;
                        break;
                    case "3000 runs on 1 thread":
                        count = 3000;
                        StatusProgressbar.IsIndeterminate = true;
                        break;
                    case "10000 runs on 1 thread":
                        count = 10000;
                        StatusProgressbar.IsIndeterminate = true;
                        break;
                }
                
                switch (warmupText)
                {
                    case "0 seconds":
                        warmup = TimeSpan.FromSeconds(0);
                        break;
                    case "2 seconds":
                        warmup = TimeSpan.FromSeconds(2);
                        break;
                    case "10 seconds":
                        warmup = TimeSpan.FromSeconds(10);
                        break;
                    case "20 seconds":
                        warmup = TimeSpan.FromSeconds(20);
                        break;
                    case "30 seconds":
                        warmup = TimeSpan.FromSeconds(30);
                        break;
                    case "1 minute":
                        warmup = TimeSpan.FromMinutes(1);
                        break;
                    case "2 minutes":
                        warmup = TimeSpan.FromMinutes(2);
                        break;
                }

                if (string.IsNullOrWhiteSpace(Url.Text))
                {
                    return;
                }

                if (!Uri.TryCreate(Url.Text.Trim(), UriKind.Absolute, out var uri))
                {
                    return;
                }

                if (Plugins.SelectedIndex == -1)
                {
                    return;
                }

                Threads.IsEnabled = false;
                Duration.IsEnabled = false;
                Url.IsEnabled = false;
                StartButton.Content = "Cancel";
                _running = true;

                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                StatusProgressbar.Value = 0;
                StatusProgressbar.Visibility = Visibility.Visible;

                List<PluginInfo> pluginList = Plugins.Tag as List<PluginInfo>;
                PluginInfo selectedPlugin = pluginList[Plugins.SelectedIndex];

                Assembly workerPluginAssembly = PluginLoader.LoadPlugin(selectedPlugin.AssemblyName);
                var workerPlugin = PluginLoader.CreateInstance(workerPluginAssembly, selectedPlugin.TypeName);

                workerPlugin.Initialize(uri);

                var worker = new Worker(workerPlugin);

                if (count.HasValue)
                {
                    _task = worker.Run(uri.ToString(), count.Value, warmup, cancellationToken);
                }
                else
                {
                    _task = worker.Run(uri.ToString(), threads, duration, warmup, cancellationToken);
                }

                _task.GetAwaiter().OnCompleted(async () =>
                {
                    await JobCompleted();
                });

                if (StatusProgressbar.IsIndeterminate)
                {
                    return;
                }

                var sw = Stopwatch.StartNew();

                while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
                {
                    await Task.Delay(500);
                    StatusProgressbar.Value = 100.0 / duration.TotalMilliseconds * sw.Elapsed.TotalMilliseconds;
                }

                if (!_running)
                {
                    return;
                }

                StatusProgressbar.IsIndeterminate = true;
                StartButton.IsEnabled = false;
            }
            else
            {
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }

        private void Urls_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
            {
                return;
            }

            StartButton_Click(sender, null);
            StartButton.Focus();
        }

        private async Task JobCompleted()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            _running = false;
            Threads.IsEnabled = true;
            Duration.IsEnabled = true;
            Url.IsEnabled = true;
            StartButton.IsEnabled = false;
            _cancellationTokenSource = null;

            var result = new ResultWindow(this);
            result.Closing += ResultWindowClosing;
            _resultWindows.Add(result);
            await result.Load(_task.Result, _baselineResult);
            result.Show();
            _task = null;
            StatusProgressbar.Visibility = Visibility.Hidden;
            StartButton.IsEnabled = true;
            StartButton.Content = "Run";
        }

        private void ResultWindowClosing(object sender, EventArgs e)
        {
            _resultWindows.Remove((ResultWindow)sender);
        }

        public void SetBaseline(ResultWindowItem baselineResult)
        {
            _baselineResult = baselineResult;

            foreach (var resultWindow in _resultWindows)
            {
                if (baselineResult != null)
                {
                    resultWindow.LoadBaseline(baselineResult);
                }
                else
                {
                    resultWindow.ClearBaseline();
                }
            }
        }
    }
}
