using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Netling.Core.Utils
{
    internal static class ThreadHelper
    {
        public static void QueueThread(int i, bool useThreadAfinity, Action action)
        {
            var thread = new Thread(() => {
                if (useThreadAfinity)
                {
                    Thread.BeginThreadAffinity();
                    var afinity = GetAfinity(i + 1, Environment.ProcessorCount);
                    GetCurrentThread().ProcessorAffinity = new IntPtr(1 << afinity);
                }

                action.Invoke();

                if (useThreadAfinity)
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

        private static int GetAfinity(int i, int cores)
        {
            var afinity = i * 2 % cores;

            if (i % cores >= cores / 2)
                afinity++;

            return afinity;
        }
    }
}
