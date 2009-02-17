using System;

namespace Rhino.ServiceBus.Messages
{
    public class Heartbeat : LoadBalancerMessage
    {
        public DateTime At { get; set; }
        public Uri From { get; set; }
    }
}
