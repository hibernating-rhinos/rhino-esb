using System;
using System.Runtime.Serialization;

namespace Rhino.ServiceBus.Exceptions
{
    [Serializable]
    public class UnboundedResultSetException : Exception
    {
        public UnboundedResultSetException()
        {
        }

        public UnboundedResultSetException(string message) : base(message)
        {
        }

        public UnboundedResultSetException(string message, Exception inner) : base(message, inner)
        {
        }

        protected UnboundedResultSetException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}