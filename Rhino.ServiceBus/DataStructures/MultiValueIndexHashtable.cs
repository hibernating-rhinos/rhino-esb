using System.Collections.Generic;
using System.Threading;

namespace Rhino.ServiceBus.DataStructures
{
    public class MultiValueIndexHashtable<TUniqueKey, TMultiKey, TVal, TExtra>
    {
        private readonly Dictionary<TMultiKey, List<TUniqueKey>> keys = new Dictionary<TMultiKey, List<TUniqueKey>>();
        private readonly Dictionary<TUniqueKey, TVal> values = new Dictionary<TUniqueKey, TVal>();
        private readonly Dictionary<TUniqueKey, TExtra> extras = new Dictionary<TUniqueKey, TExtra>();

        private readonly ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();

        public void Add(TUniqueKey uniqueKey, TMultiKey multiKey, TVal val, TExtra extra)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                List<TUniqueKey> value;
                if (keys.TryGetValue(multiKey, out value) == false)
                {
                    keys[multiKey] = value = new List<TUniqueKey>();
                }
                value.Add(uniqueKey);
                values[uniqueKey] = val;
                extras[uniqueKey] = extra;
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        public bool TryGet(TUniqueKey uniqueKey, out TVal val)
        {
            readerWriterLockSlim.EnterReadLock();
            try
            {
                return values.TryGetValue(uniqueKey, out val);
            }
            finally
            {
                readerWriterLockSlim.ExitReadLock();
            }
        }

        public bool TryGet(TMultiKey multiKey, out List<TVal> val)
        {
            readerWriterLockSlim.EnterReadLock();
            try
            {
                val = new List<TVal>();
                List<TUniqueKey> keysList;
                if (keys.TryGetValue(multiKey, out keysList) == false)
                    return false;
                var keysToRemove = new List<TUniqueKey>();
                foreach (var key in keysList)
                {
                    TVal value;
                    if (values.TryGetValue(key, out value))
                        val.Add(value);
                    else
                        keysToRemove.Add(key);
                }
                foreach (var key in keysToRemove)
                {
                    keysList.Remove(key);
                }
                return val.Count > 0;
            }
            finally
            {
                readerWriterLockSlim.ExitReadLock();
            }
        }

        public void Remove(TMultiKey multiKey)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                List<TUniqueKey> uniqueKeys;
                if (keys.TryGetValue(multiKey, out uniqueKeys) == false)
                    return;
                keys.Remove(multiKey);
                foreach (var key in uniqueKeys)
                {
                    TExtra ignored;
                    TryRemove(key, out ignored);
                }
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        public bool TryRemove(TUniqueKey uniqueKey, out TExtra extra)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                values.Remove(uniqueKey);
                var value = extras.TryGetValue(uniqueKey, out extra);
                if (value)
                    extras.Remove(uniqueKey);
                return value;
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }
    }
}