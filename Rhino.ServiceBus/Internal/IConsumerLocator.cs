namespace Rhino.ServiceBus.Internal
{
    public interface IConsumerLocator
    {
        object[] GatherConsumers(object message);
    }
}