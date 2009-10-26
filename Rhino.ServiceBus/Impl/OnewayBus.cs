using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Impl
{
    public class OnewayBus : IOnewayBus
    {
        private readonly MessageOwnersSelector messageOwners;
        private readonly IMessageBuilder messageBuilder;

        public OnewayBus(MessageOwner[] messageOwners, IMessageBuilder messageBuilder)
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