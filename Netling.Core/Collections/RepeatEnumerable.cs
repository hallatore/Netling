using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Netling.Core.Collections
{
    public class RepeatEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _inner;

        public RepeatEnumerable()
        {
            _inner = new T[] { };
        }

        public RepeatEnumerable(T one)
        {
            _inner = new T[] { one };
        }

        public RepeatEnumerable(params T[] many)
        {
            _inner = many;
        }

        public RepeatEnumerable(IEnumerable<T> many)
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
    }
}
