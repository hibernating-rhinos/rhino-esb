using System;
using System.Runtime.Serialization;

namespace Rhino.ServiceBus.Exceptions
{
    [Serializable]
    public class SubscriptionException : Exception
    {
        public SubscriptionException()
        {
        }

        public SubscriptionException(string message) : base(message)
        {
        }

        public SubscriptionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SubscriptionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}