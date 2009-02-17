using System;

namespace Rhino.ServiceBus.Messages
{
    public class MessageSentMessage
    {
        public Uri Source { get; set; }

        public Uri Destination { get; set; }

        public DateTime Timestamp { get; set; }

        public string MessageType { get; set; }

        public Guid MessageId { get; set; }

        public object Message { get; set; }
    }
}