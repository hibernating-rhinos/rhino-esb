using System;

namespace Rhino.ServiceBus.Internal
{
    public interface IMessageBuilder<T>
    {
        event Action<T> MessageBuilt;
        T BuildFromMessageBatch(params object[] msgs);
        void Initialize(Endpoint source);
    }
}