using System;

namespace Rhino.ServiceBus.Messages
{
    public class SerializationErrorMessage : ILogMessage
    {
        public Guid MessageId { get; set; }

        public string Error { get; set; }

        public Uri Source { get; set; }
    }
}