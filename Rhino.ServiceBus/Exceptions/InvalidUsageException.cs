using System;
using System.Runtime.Serialization;

namespace Rhino.ServiceBus.Exceptions
{
    [Serializable]
    public class InvalidUsageException : Exception
    {
        public InvalidUsageException()
        {
        }

        public InvalidUsageException(string message)
            : base(message)
        {
        }

        public InvalidUsageException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected InvalidUsageException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}