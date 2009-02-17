namespace Rhino.ServiceBus.Sagas
{
    public interface InitiatedBy<TMsg> : ConsumerOf<TMsg>
    {
    }
}