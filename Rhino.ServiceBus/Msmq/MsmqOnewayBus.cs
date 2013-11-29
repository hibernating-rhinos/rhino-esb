using System.Messaging;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Msmq
{
    public class MsmqOnewayBus : IOnewayBus
    {
        private readonly MessageOwnersSelector messageOwners;
        private readonly IMessageBuilder<Message> messageBuilder;
        private readonly bool _useDeadLetterQueue;

        public MsmqOnewayBus(MessageOwner[] messageOwners, IMessageBuilder<Message> messageBuilder, bool useDeadLetterQueue)
        {
            this.messageOwners = new MessageOwnersSelector(messageOwners, new EndpointRouter());
            this.messageBuilder = messageBuilder;
            _useDeadLetterQueue = useDeadLetterQueue;
        }

        public void Send(params object[] msgs)
        {
            var endpoint = messageOwners.GetEndpointForMessageBatch(msgs);
            var messageInformation = new OutgoingMessageInformation
            {
                Destination = endpoint,
                Messages = msgs,
                UseDeadLetterQueue = _useDeadLetterQueue
            };
            using(var queue = endpoint.InitalizeQueue())
            {
                var message = messageBuilder.BuildFromMessageBatch(messageInformation);
                queue.SendInSingleTransaction(message);
            }
        }
    }
}