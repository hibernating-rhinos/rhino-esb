namespace Rhino.ServiceBus
{
    public interface IMessageBuilder<T>
    {
        T BuildFromMessageBatch(params object[] msgs);
        void Initialize(Endpoint source);
    }
}