namespace Rhino.ServiceBus
{
    public interface ICustomizeOutgoingMessages
    {
        void Customize(OutgoingMessageInformation messageInformation);
    }
}