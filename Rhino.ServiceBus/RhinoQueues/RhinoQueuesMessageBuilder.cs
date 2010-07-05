using System;
using Rhino.Queues;

namespace Rhino.ServiceBus.RhinoQueues
{
    public class RhinoQueuesMessageBuilder : IMessageBuilder<MessagePayload>
    {
        public MessagePayload BuildFromMessageBatch(params object[] msgs)
        {
            throw new NotImplementedException();
        }
    }
}