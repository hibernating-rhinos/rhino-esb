using System;

namespace Rhino.ServiceBus.Internal
{
    public interface IMessageBuilder<T>
    {
        event Action<T> MessageBuilt;
        T BuildFromMessageBatch(OutgoingMessageInformation messageInformation);
        void Initialize(Endpoint source);
    }
}