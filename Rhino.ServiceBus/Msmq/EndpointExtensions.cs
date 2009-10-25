using System;
using System.Messaging;
using Rhino.ServiceBus.Exceptions;

namespace Rhino.ServiceBus.Msmq
{
    public static class EndpointExtensions
    {
        public static OpenedQueue InitalizeQueue(this Endpoint endpoint)
        {
            try
            {
                return MsmqUtil.GetQueuePath(endpoint).Open(QueueAccessMode.SendAndReceive);
            }
            catch (Exception e)
            {
                throw new TransportException(
                    "Could not open queue: " + endpoint + Environment.NewLine +
                    "Queue path: " + MsmqUtil.GetQueuePath(endpoint) + Environment.NewLine +
                    "Did you forget to create the queue or disable the queue initialization module?", e);
            }

        }
    }
}