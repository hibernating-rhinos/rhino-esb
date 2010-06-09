using System;

namespace Rhino.ServiceBus.Messages
{
    public class ReadyForWorkQueueUri : LoadBalancerMessage
    {
        public Uri Endpoint { get; set; }
    }
}
