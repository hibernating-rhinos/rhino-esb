using System;
using Rhino.Queues;
using Rhino.Queues.Model;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.RhinoQueues
{
    internal class RhinoQueueCurrentMessageInformation : CurrentMessageInformation
    {
        public Uri ListenUri { get; set; }
        public IQueue Queue { get; set; }
        public Message TransportMessage { get; set; }
    }
}