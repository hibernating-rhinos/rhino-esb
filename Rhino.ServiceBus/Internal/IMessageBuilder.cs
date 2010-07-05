namespace Rhino.ServiceBus.Internal
{
    public interface IMessageBuilder<T>
    {
        T BuildFromMessageBatch(params object[] msgs);
        void Initialize(Endpoint source);
    }
}