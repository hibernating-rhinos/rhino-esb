using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Rhino.ServiceBus.DataStructures
{
    public class LeastRecentlyUsedSet<T> : IEnumerable<T>
    {
        #region Delegates

        public delegate void WriteAction(Action<T> add, Action<T> remove);

        #endregion

        private readonly LinkedList<T> inOrder = new LinkedList<T>();
        private readonly int maxEntries;
        private readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();
        private readonly HashSet<T> set;

        public LeastRecentlyUsedSet(IEqualityComparer<T> comparer)
            : this(comparer, 100)
        {
        }

        public LeastRecentlyUsedSet(int maxEntries)
            : this(EqualityComparer<T>.Default, maxEntries)
        {
            this.maxEntries = maxEntries;
        }

        public LeastRecentlyUsedSet(IEqualityComparer<T> comparer, int maxEntries)
        {
            set = new HashSet<T>(comparer);
            this.maxEntries = maxEntries;
        }

        public LeastRecentlyUsedSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            readerWriterLock.EnterReadLock();
            try
            {
                return set.ToList().GetEnumerator();
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            } 
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public void Write(WriteAction action)
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                action(Add, Remove);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        private void Add(T item)
        {
            set.Add(item);
            inOrder.AddLast(item);
            if (inOrder.Count <= maxEntries)
                return;

            set.Remove(inOrder.First.Value);
            inOrder.RemoveFirst();
        }

        public bool Contains(T item)
        {
            readerWriterLock.EnterReadLock();
            try
            {
                return set.Contains(item);
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }

        private void Remove(T item)
        {
            set.Remove(item);
        }
    }
}