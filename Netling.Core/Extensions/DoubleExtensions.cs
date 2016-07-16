using System;
using System.Linq;

namespace Netling.Core.Extensions
{
    internal static class DoubleExtensions
    {
        public static double GetMedian(this double[] source)
        {
            var count = source.Length;

            if (count == 0)
                return 0;

            if (count % 2 != 0)
                return source[count / 2];

            var a = source[count / 2 - 1];
            var b = source[count / 2];
            return (a + b) / 2;
        }

        public static double GetStdDev(this double[] source)
        {
            if (source.Length <= 0)
                return 0;

            var avg = source.Average();
            var sum = source.Sum(d => Math.Pow(d - avg, 2));
            return Math.Sqrt(sum / (source.Length - 1));
        }
    }
}
