namespace Rhino.ServiceBus.Sagas
{
    public interface Orchestrates<TMsg> : ConsumerOf<TMsg>
        where TMsg: ISagaMessage
    {
    }
}