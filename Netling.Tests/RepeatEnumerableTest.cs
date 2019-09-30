using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Netling.Core.Collections;
using NUnit.Framework;

namespace Netling.Tests
{
    [TestFixture]
    public class RepeatEnumerableTest
    {
        [Test]
        public void RepeatEnumerable_Handles_Empty()
        {
            var re = new RepeatEnumerable<int>();

            foreach (var r in re)
            {
                Assert.Fail("Empty RepeatEnumerable Should Not Advance.");
            }
        }

        [Test]
        public void RepeatEnumerable_Handles_One()
        {
            var re = new RepeatEnumerable<int>(1);

            var results = new List<int>();
            var count = 0;
            foreach (var r in re)
            {
                if (count > 4)
                {
                    break;
                }

                results.Add(r);
                count++;
            }
            Assert.True(results.Count == 5, "Count is wrong.");
            Assert.True(results.Count(v => v == 1) == 5, "Content is wrong.");
        }

        [Test]
        public void RepeatEnumerable_Handles_Many()
        {
            var re = new RepeatEnumerable<int>(1, 2, 3);

            var results = new List<int>();
            var count = 0;
            foreach (var r in re)
            {
                if (count > 8)
                {
                    break;
                }

                results.Add(r);
                count++;
            }
            Assert.True(results.Count == 9, "Count is wrong.");
            Assert.True(results.SequenceEqual(new int[] {1, 2, 3, 1, 2, 3, 1, 2, 3 }), "Content is wrong.");
        }
    }
}
