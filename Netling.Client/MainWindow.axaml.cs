﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Netling.Core;
using Netling.Core.Models;
using Netling.Core.SocketWorker;
using Netling.Core.SocketWorker.Performance;

namespace Netling.Client
{
    public partial class MainWindow : Window
    {
        private bool _running;
        private CancellationTokenSource _cancellationTokenSource;
        private Task<WorkerResult> _task;
        private readonly List<ResultWindow> _resultWindows;
        private ResultWindowItem _baselineResult;

        private readonly IList<KeyValuePair<int, string>> _threadItems = new List<KeyValuePair<int, string>>();

        public MainWindow()
        {
            _resultWindows = new List<ResultWindow>();
            InitializeComponent();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture.NumberFormat.NumberGroupSeparator = " ";
            Opened += OnLoaded;

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void OnLoaded(object sender, EventArgs routedEventArgs)
        {
            for (var i = 1; i <= Environment.ProcessorCount; i++)
            {
                _threadItems.Add(new KeyValuePair<int, string>(i, i.ToString()));
            }

            for (var i = 2; i <= 20; i++)
            {
                _threadItems.Add(new KeyValuePair<int, string>(Environment.ProcessorCount * i, $"{Environment.ProcessorCount * i} - ({i} per core)"));
            }

            Threads.Items = _threadItems;
            Threads.SelectedIndex = 0;
            Url.Focus();
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_running)
            {
                var duration = default(TimeSpan);
                int? count = null;
                var threads = Convert.ToInt32(((KeyValuePair<int, string>)Threads.SelectedItem).Key);
                var durationText = (string)((ComboBoxItem)Duration.SelectedItem)?.Content ?? "10 seconds";
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

                if (string.IsNullOrWhiteSpace(Url.Text))
                {
                    return;
                }

                if (!Uri.TryCreate(Url.Text.Trim(), UriKind.Absolute, out var uri))
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
                StatusProgressbar.IsVisible = true;

                var worker = new Worker(new SocketWorkerJob(uri));

                if (count.HasValue)
                {
                    _task = worker.Run(uri.ToString(), count.Value, cancellationToken);
                }
                else
                {
                    _task = worker.Run(uri.ToString(), threads, duration, cancellationToken);
                }

                _task.GetAwaiter().OnCompleted(async () => { await JobCompleted(); });

                if (StatusProgressbar.IsIndeterminate)
                {
                    return;
                }

                var sw = Stopwatch.StartNew();

                while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
                {
                    await Task.Delay(500, cancellationToken);
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
                if (!_cancellationTokenSource.IsCancellationRequested)
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
            if (!_cancellationTokenSource.IsCancellationRequested)
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
            StatusProgressbar.IsVisible = false;
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