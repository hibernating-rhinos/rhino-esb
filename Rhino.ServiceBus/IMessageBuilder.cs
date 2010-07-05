using System.Messaging;

namespace Rhino.ServiceBus
{
    public interface IMessageBuilder<T>
    {
        T BuildFromMessageBatch(params object[] msgs);
    }
}