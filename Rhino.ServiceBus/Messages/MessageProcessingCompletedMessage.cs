using System;

namespace Rhino.ServiceBus.Messages
{
    public class MessageProcessingCompletedMessage
    {
        public Uri Source { get; set; }

        public DateTime Timestamp { get; set; }

        public string MessageType { get; set; }

        public Guid MessageId { get; set; }
    }
}