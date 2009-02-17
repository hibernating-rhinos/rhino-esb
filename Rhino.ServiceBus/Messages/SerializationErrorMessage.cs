using System;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Messages
{
    public class SerializationErrorMessage
    {
        public Guid MessageId { get; set; }

        public string Error { get; set; }

        public Uri Source { get; set; }
    }
}