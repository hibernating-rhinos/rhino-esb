using System.Messaging;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Msmq.TransportActions
{
    public interface ITransportAction
    {
        void Init(IMsmqTransport transport);

        bool CanHandlePeekedMessage(Message message);
        bool HandlePeekedMessage(IMsmqTransport transport, OpenedQueue queue, Message message);
    }
}