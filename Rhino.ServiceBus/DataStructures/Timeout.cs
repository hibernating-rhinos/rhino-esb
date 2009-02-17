using System;
using System.Threading;

namespace Rhino.ServiceBus.DataStructures
{
    public class Timeout
    {
        private readonly ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim();

        private DateTime lastValue;

        private readonly TimeSpan timeout;

        public Timeout(TimeSpan timeout)
        {
            this.timeout = timeout;
        } 

        public void SetHeartbeat(DateTime value)
        {
            readerWriterLockSlim.EnterWriteLock();
            try
            {
                lastValue = value;
            }
            finally
            {
                readerWriterLockSlim.ExitWriteLock();
            }
        }

        public bool CheckTimestamp()
        {
            readerWriterLockSlim.EnterReadLock();
            try
            {
                return (DateTime.Now - lastValue) > timeout;
            }
            finally
            {
                readerWriterLockSlim.ExitReadLock();
            }
        }
    }
}