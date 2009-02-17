using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Rhino.ServiceBus.DataStructures
{
    public class Set<T>
    {
        private readonly HashSet<T> set = new HashSet<T>();
        private readonly ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();

        public bool Add(T value)
        {
            readerWriterLockSlim.EnterReadLock();
            try
            {
                if(set.Contains(value))
                    return false;
            }
            finally
            {
                readerWriterLockSlim.ExitReadLock();
            }

            readerWriterLockSlim.EnterWriteLock();
            try
            {
                return set.Add(value);
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        public bool Remove(T value)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                return set.Remove(value);
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        public T[] GetValues()
        {
            readerWriterLockSlim.EnterReadLock();
            try
            {
                return set.ToArray();
            }
            finally
            {
                readerWriterLockSlim.ExitReadLock();
            }
        }
    }
}