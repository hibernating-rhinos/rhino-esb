using System;
using System.Runtime.Serialization;

namespace Rhino.ServiceBus.Exceptions
{
    [Serializable]
    public class MessagePublicationException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public MessagePublicationException()
        {
        }

        public MessagePublicationException(string message) : base(message)
        {
        }

        public MessagePublicationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MessagePublicationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}