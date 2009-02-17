using System;
using System.Runtime.Serialization;

namespace Rhino.ServiceBus.Exceptions
{
    [Serializable]
    public class OptimisticConcurrencyException : Exception
    {
        public OptimisticConcurrencyException()
        {
        }

        public OptimisticConcurrencyException(string message) : base(message)
        {
        }

        public OptimisticConcurrencyException(string message, Exception inner) : base(message, inner)
        {
        }

        protected OptimisticConcurrencyException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}