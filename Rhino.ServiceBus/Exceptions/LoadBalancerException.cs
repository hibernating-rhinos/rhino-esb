using System;
using System.Runtime.Serialization;

namespace Rhino.ServiceBus.Exceptions
{
    [Serializable]
    public class LoadBalancerException : Exception
    {
        public LoadBalancerException()
        {
        }

        public LoadBalancerException(string message) : base(message)
        {
        }

        public LoadBalancerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected LoadBalancerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}