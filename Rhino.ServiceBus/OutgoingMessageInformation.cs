using System;
using System.Collections.Specialized;

namespace Rhino.ServiceBus
{
    public class OutgoingMessageInformation
    {
        public DateTime? DeliverBy { get; set; }

        /// <summary>
        /// The destination the messages will be sent to.  This may be null if the 
        /// messages are being sent to multiple endpoints.
        /// </summary>
        public Endpoint Destination { get; set; }

        public NameValueCollection Headers { get; set; }
        public int? MaxAttempts { get; set; }
        public object[] Messages { get; set; }

        /// <summary>
        /// The current endpoint.  This may be null on a one-way bus.
        /// </summary>
        public Endpoint Source { get; set; }
    }
}