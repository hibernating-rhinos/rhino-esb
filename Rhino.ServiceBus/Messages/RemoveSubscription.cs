namespace Rhino.ServiceBus.Messages
{
    public class RemoveSubscription : AdministrativeMessage
    {
        public string Type { get; set; }
        public Endpoint Endpoint { get; set; }
    }
}