using System;

namespace Rhino.ServiceBus.Messages
{
    public class NewEndpointPersisted : LoadBalancerMessage
    {
        public Uri PersistedEndpoint { get; set; }
    }
}