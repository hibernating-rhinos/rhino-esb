using System;
using System.Transactions;
using Rhino.Queues;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.RhinoQueues
{
    [CLSCompliant(false)]
    public class RhinoQueuesOneWayBus : RhinoQueuesTransport, IOnewayBus
    {
        private MessageOwnersSelector messageOwners;
        public static readonly Uri NullEndpoint = new Uri(string.Format("null://nowhere:{0}/middle", ANY_AVAILABLE_PORT));
        public RhinoQueuesOneWayBus(MessageOwner[] messageOwners, IMessageSerializer messageSerializer, string path, bool enablePerformanceCounters, IMessageBuilder<MessagePayload> messageBuilder, QueueManagerConfiguration queueManagerConfiguration)
            : base(NullEndpoint, new EndpointRouter(), messageSerializer, 1, path, IsolationLevel.ReadCommitted, 5, enablePerformanceCounters, messageBuilder, queueManagerConfiguration)

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