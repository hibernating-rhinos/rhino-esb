using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.RhinoQueues
{
    public class RhinoQueuesOneWayBus : IOnewayBus
    {
        private MessageOwnersSelector messageOwners;
        private ITransport transport;

        public RhinoQueuesOneWayBus(MessageOwner[] messageOwners, ITransport transport)
        {
            this.messageOwners = new MessageOwnersSelector(messageOwners, new EndpointRouter());
            this.transport = transport;
            this.transport.Start();
        }

        public void Send(params object[] msgs)
        {
            transport.Send(messageOwners.GetEndpointForMessageBatch(msgs), msgs);
        }
    }
}