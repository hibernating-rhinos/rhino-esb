using System;

namespace Rhino.ServiceBus.Messages
{
    public class AcceptingWork : LoadBalancerMessage
    {
        public Uri Endpoint { get; set; }
    }
}