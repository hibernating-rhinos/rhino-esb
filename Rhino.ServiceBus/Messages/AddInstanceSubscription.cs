using System;

namespace Rhino.ServiceBus.Messages
{
    public class AddInstanceSubscription : AdministrativeMessage
    {
        public string Type { get; set; }
        public Guid InstanceSubscriptionKey { get; set; }
        public string Endpoint { get; set; }
    }
}