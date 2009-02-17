using System.Messaging;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Msmq.TransportActions
{
    public abstract class AbstractTransportAction : ITransportAction
    {
        public abstract MessageType HandledType { get; }

        public virtual void Init(IMsmqTransport transport)
        {
            
        }

        public bool CanHandlePeekedMessage(Message message)
        {
            var messagType = (MessageType) message.AppSpecific;
            return messagType == HandledType;
        }

        public abstract bool HandlePeekedMessage(IMsmqTransport transport, OpenedQueue queue, Message message);
    }
}