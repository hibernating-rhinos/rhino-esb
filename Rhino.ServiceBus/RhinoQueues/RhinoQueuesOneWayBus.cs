using System;
using System.IO;
using System.Transactions;
using Rhino.Queues;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.RhinoQueues
{
    [CLSCompliant(false)]
    public class RhinoQueuesOneWayBus : RhinoQueuesTransport,IOnewayBus
    {
        private MessageOwnersSelector messageOwners;
        public static readonly Uri NullEndpoint = new Uri("null://nowhere:24689/middle");
        public RhinoQueuesOneWayBus(Uri endpoint, MessageOwner[] messageOwners, IMessageSerializer messageSerializer, IMessageBuilder<MessagePayload> messageBuilder)
            : base(endpoint??NullEndpoint, new EndpointRouter(), messageSerializer, 1, Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "one_way.esent"), IsolationLevel.ReadCommitted,5,messageBuilder)

        {
            this.messageOwners = new MessageOwnersSelector(messageOwners, new EndpointRouter());
            Start();
        }

        public void Send(params object[] msgs)
        {
            base.Send(messageOwners.GetEndpointForMessageBatch(msgs), msgs);
        }

       
    }
}