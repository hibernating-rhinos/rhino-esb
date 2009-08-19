namespace Rhino.ServiceBus.Sagas
{
    public interface Orchestrates<TMsg> : ConsumerOf<TMsg>
    {
    }
}
