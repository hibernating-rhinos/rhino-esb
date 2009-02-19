using System.Diagnostics;

namespace Rhino.ServiceBus.Messages
{
    public class AddSubscription : AdministrativeMessage
    {
        public string Type { get; set; }
        public Endpoint Endpoint { get; set; }
    }
}