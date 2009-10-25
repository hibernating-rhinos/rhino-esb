using System;
using System.Collections;
using System.Linq;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Internal;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Impl
{
    public class MessageOwnersSelector
    {
        private readonly MessageOwner[] messageOwners;
        private readonly IEndpointRouter endpointRouter;

        public MessageOwnersSelector(
            MessageOwner[] messageOwners,
            IEndpointRouter endpointRouter)
        {
            this.messageOwners = messageOwners;
            this.endpointRouter = endpointRouter;
        }

        public Endpoint GetEndpointForMessageBatch(object[] messages)
        {
            if (messages == null)
                throw new ArgumentNullException("messages");

            if (messages.Length == 0)
                throw new MessagePublicationException("Cannot send empty message batch");

            var messageOwner = messageOwners
                .Where(x => x.IsOwner(messages[0].GetType()))
                .FirstOrDefault();

            if (messageOwner == null)
                throw new MessagePublicationException("Could not find no message owner for " + messages[0]);

            var endpoint = endpointRouter.GetRoutedEndpoint(messageOwner.Endpoint);
            endpoint.Transactional = messageOwner.Transactional;
            return endpoint;
        }

        public IEnumerable<MessageOwner> Of(Type type)
        {
            return messageOwners.Where(x => x.IsOwner(type));
        }

        public IEnumerable<MessageOwner> NotOf(Type type)
        {
            return messageOwners.Where(x => x.IsOwner(type) == false);
        }
    }
}