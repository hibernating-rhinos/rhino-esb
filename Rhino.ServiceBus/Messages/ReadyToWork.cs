using System;

namespace Rhino.ServiceBus.Messages
{
    public class ReadyToWork : LoadBalancerMessage
    {
        public Uri Endpoint { get; set; }
    }
}