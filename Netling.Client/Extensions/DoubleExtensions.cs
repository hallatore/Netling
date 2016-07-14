using System;
using System.Collections.Generic;
using System.Linq;

namespace Netling.Client.Extensions
{
    public static class DoubleExtensions
    {
        public static double GetMedian(this List<double> source)
        {
            var temp = source.OrderBy(s => s).ToList();

            var count = temp.Count;
            if (count == 0)
            {
                return 0;
            }

            if (count % 2 == 0)
            {
                var a = temp[count / 2 - 1];
                var b = temp[count / 2];
                return (a + b) / 2;
            }

            return temp[count / 2];
        }

        public static double GetStdDev(this List<double> source)
        {
            if (source.Count <= 0)
                return 0;

            var avg = source.Average();
            var sum = source.Sum(d => Math.Pow(d - avg, 2));
            return Math.Sqrt(sum / (source.Count - 1));
        }
    }
}
