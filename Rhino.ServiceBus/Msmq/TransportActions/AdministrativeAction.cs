using System;
using System.Messaging;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Msmq.TransportActions
{
    public class AdministrativeAction : AbstractTransportAction
    {
        private IMsmqTransport transport;

        public override MessageType HandledType
        {
            get { return MessageType.AdministrativeMessageMarker; }
        }

        public override void Init(IMsmqTransport parentTransport, OpenedQueue queue)
        {
            transport = parentTransport;
        }

        public override bool HandlePeekedMessage(IMsmqTransport transport1, OpenedQueue queue, Message message)
        {
            Func<CurrentMessageInformation, bool> messageRecieved = information =>
            {
                transport.RaiseAdministrativeMessageArrived(information);

                return true;
            };

            transport.ReceiveMessageInTransaction(
                queue, message.Id,
                messageRecieved,
                transport.RaiseAdministrativeMessageProcessingCompleted);

            return true;
        }
    }
}