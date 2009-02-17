using System;
using System.Runtime.Serialization;

namespace Rhino.ServiceBus.Exceptions
{
    [Serializable]
    public class InvalidDeploymentException : Exception
    {
        public InvalidDeploymentException()
        {
        }

        public InvalidDeploymentException(string message) : base(message)
        {
        }

        public InvalidDeploymentException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidDeploymentException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}