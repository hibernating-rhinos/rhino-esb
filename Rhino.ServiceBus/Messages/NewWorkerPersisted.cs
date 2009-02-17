using System;

namespace Rhino.ServiceBus.Messages
{
    public class NewWorkerPersisted : LoadBalancerMessage
    {
        public Uri Endpoint { get; set; }
    }
}