using System;

namespace Rhino.ServiceBus.Messages
{
    public class MessageProcessingFailedMessage
    {
        public Uri Source { get; set; }

        public DateTime Timestamp { get; set; }

        public string MessageType { get; set; }

        public Guid MessageId { get; set; }

        public object Message { get; set; }

        public string ErrorText { get; set; }
    }
}