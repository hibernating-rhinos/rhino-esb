using System;
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
        public RhinoQueuesOneWayBus(MessageOwner[] messageOwners, IMessageSerializer messageSerializer, string path, bool enablePerformanceCounters, IMessageBuilder<MessagePayload> messageBuilder)
            : base(NullEndpoint, new EndpointRouter(), messageSerializer, 1, path, IsolationLevel.ReadCommitted, 5, enablePerformanceCounters, messageBuilder)

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