using System.Messaging;
using Rhino.ServiceBus.Internal;
using MessageType=Rhino.ServiceBus.Transport.MessageType;

namespace Rhino.ServiceBus.Msmq.TransportActions
{
    public abstract class AbstractTransportAction : IMsmqTransportAction
    {
        public abstract MessageType HandledType { get; }

        public virtual void Init(IMsmqTransport transport, OpenedQueue queue)
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