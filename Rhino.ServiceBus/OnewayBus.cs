using Rhino.ServiceBus.Impl;
using System.Linq;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus
{
    public class OnewayBus : IOnewayBus
    {
        private readonly MessageOwnersSelector messageOwners;
        private readonly MessageBuilder messageBuilder;

        public OnewayBus(MessageOwner[] messageOwners, MessageBuilder messageBuilder)
        {
            this.messageOwners = new MessageOwnersSelector(messageOwners, new EndpointRouter());
            this.messageBuilder = messageBuilder;
        }

        public void Send(params object[] msgs)
        {
            var endpoint = messageOwners.GetEndpointForMessageBatch(msgs);
            using(var queue = endpoint.InitalizeQueue())
            {
                var message = messageBuilder.GenerateMsmqMessageFromMessageBatch(msgs);
                queue.SendInSingleTransaction(message);
            }
        }
    }
}