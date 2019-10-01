using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

namespace Netling.Core.Collections
{
    public class Stream<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private readonly IEnumerable<T> _inner;

        public int Count => _inner.Count();

        public IEnumerable<T> Once()
        {
            return _inner;
        }

        public Stream()
        {
            _inner = new T[] { };
        }

        public Stream(T one)
        {
            _inner = new T[] { one };
        }

        public Stream(params T[] many)
        {
            _inner = many;
        }

        public Stream(IEnumerable<T> many)
        {
            _inner = many;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        protected IEnumerator<T> GetEnumeratorImpl()
        {
            var iter = _inner.GetEnumerator();
            if (!iter.MoveNext())
            {
                yield break;
            }
            while (true)
            {
                yield return iter.Current;
                if (!iter.MoveNext())
                {
                    iter.Reset();
                    iter.MoveNext();
                }
            }
        }

        public IEnumerable<T> Take(IScaleOptions opts)
        {
            return _inner.Take(this.Count(opts));
        }

        public TimeSpan Frequency(IScaleOptions opts)
        {
            return TimeSpan.FromMilliseconds(Count / (opts.Threads * opts.Concurrency));
        }
    }

    public static class StreamExtension
    {
        public static int Count<T>(this Stream<T> s)
        {
            return s.Count;
        }

        public static int Count<T>(this Stream<T> s, IScaleOptions opts)
        {
            return s.Count * opts.Threads * opts.Concurrency;
        }
    }
}
