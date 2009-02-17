using System;

namespace Rhino.ServiceBus.Impl
{
    public class CurrentMessageInformation
    {
        public string TransportMessageId { get; set; }

        public Guid MessageId { get; set; }

        public Uri Source { get; set; }

        public Uri Destination { get; set; }

        public object Message { get; set; }

        public object[] AllMessages { get; set; }
    }
}
