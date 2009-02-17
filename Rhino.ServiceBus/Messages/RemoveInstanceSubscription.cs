using System;

namespace Rhino.ServiceBus.Messages
{
    public class RemoveInstanceSubscription : AdministrativeMessage
    {
        public string Type { get; set; }
        public Guid InstanceSubscriptionKey { get; set; }
        public string Endpoint { get; set; }
    }
}