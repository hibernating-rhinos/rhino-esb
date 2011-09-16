using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Rhino.ServiceBus.DataStructures
{
    public class OrderedList<TKey, TVal> 
        where TKey : IComparable<TKey>
    {
        private readonly SortedList<TKey, List<TVal>> innerList = new SortedList<TKey, List<TVal>>();
        private readonly ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();

        public class Reader
        {
            protected OrderedList<TKey, TVal> parent;

            public Reader(OrderedList<TKey, TVal> parent)
            {
                this.parent = parent;
            }

            public bool TryGetValue(TKey key, out List<TVal> val)
            {
                return parent.innerList.TryGetValue(key, out val);
            }

            public IEnumerable<TVal> EnumerateUntil(TKey key)
            {
                foreach (var kvp in parent.innerList)
                {
                    if(kvp.Key.CompareTo(key)>=0)
                        yield break;
                    foreach (TVal val in kvp.Value)
                        yield return val;
                }
            }

            public bool HasAnyBefore(TKey key)
            {
                return EnumerateUntil(key)
                    .GetEnumerator()
                    .MoveNext();
            }
        }

        public class Writer : Reader
        {
            public Writer(OrderedList<TKey, TVal> parent)
                : base(parent)
            {
            }

            public void Add(TKey key, TVal val)
            {
                List<TVal> list;
                if (parent.innerList.TryGetValue(key, out list) == false)
                {
                    list = new List<TVal>();
                    parent.innerList.Add(key, list);
                }

                list.Add(val);
            }

            public bool Remove(TKey key)
            {
                return parent.innerList.Remove(key);
            }

            public bool TryRemoveFirstUntil(TKey key, out KeyValuePair<TKey, List<TVal>> pair)
            {
                pair = new KeyValuePair<TKey, List<TVal>>();
                if (parent.innerList.Count == 0)
                    return false;

                pair = parent.innerList.First();
                if (pair.Key.CompareTo(key) >= 0)
                    return false;

                parent.innerList.RemoveAt(0);
                return true;
            }
        }

        public void Write(Action<Writer> action)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                action(new Writer(this));
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        public void Read(Action<Reader> read)
        {
            readerWriterLockSlim.EnterReadLock();
            try
            {
                read(new Reader(this));
            }
            finally
            {
                readerWriterLockSlim.ExitReadLock();
            }
        }
    }
}