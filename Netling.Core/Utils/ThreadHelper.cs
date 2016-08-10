using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Netling.Core.Utils
{
    internal static class ThreadHelper
    {
        public static void QueueThread(int i, bool useThreadAffinity, Action<int> action)
        {
            var thread = new Thread(() => {
                if (useThreadAffinity)
                {
                    Thread.BeginThreadAffinity();
                    var affinity = GetAffinity(i + 1, Environment.ProcessorCount);
                    GetCurrentThread().ProcessorAffinity = new IntPtr(1 << affinity);
                }

                action.Invoke(i);

                if (useThreadAffinity)
                    Thread.EndThreadAffinity();
            });
            thread.Start();
        }

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        private static ProcessThread GetCurrentThread()
        {
            var id = GetCurrentThreadId();
            return
                (from ProcessThread th in System.Diagnostics.Process.GetCurrentProcess().Threads
                 where th.Id == id
                 select th).Single();
        }

        private static int GetAffinity(int i, int cores)
        {
            var affinity = i * 2 % cores;

            if (i % cores >= cores / 2)
                affinity++;

            return affinity;
        }
    }
}
