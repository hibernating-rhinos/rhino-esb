using System;
using System.Diagnostics;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Transport
{
    public static class TransportUtil
    {
        public static TimeSpan GetTransactionTimeout()
        {
            if (Debugger.IsAttached)
                return TimeSpan.FromMinutes(45);
            return TimeSpan.Zero;
        }

        public static bool ProcessSingleMessage(
            CurrentMessageInformation currentMessageInformation,
            Func<CurrentMessageInformation, bool> messageRecieved)
        {
            if (messageRecieved == null)
                return false;
            foreach (Func<CurrentMessageInformation, bool> func in messageRecieved.GetInvocationList())
            {
                if (func(currentMessageInformation))
                {
                    return true;
                }
            }
            return false;
        }

    }
}