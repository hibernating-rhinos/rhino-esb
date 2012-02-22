using System;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Internal
{
    public interface ITransport : IDisposable
    {
        void Start();

        Endpoint Endpoint { get; }
        int ThreadCount { get; }

        /// <summary>
        /// The information for the message currently being received and handled by the transport.
        /// </summary>
        CurrentMessageInformation CurrentMessageInformation { get; }

        void Send(Endpoint destination, object[] msgs);
        void Send(Endpoint endpoint, DateTime processAgainAt, object[] msgs);

        void Reply(params object[] messages);

        event Action<CurrentMessageInformation> MessageSent;

        event Func<CurrentMessageInformation,bool> AdministrativeMessageArrived;
        
        event Func<CurrentMessageInformation, bool> MessageArrived;

        event Action<CurrentMessageInformation, Exception> MessageSerializationException;
        
        event Action<CurrentMessageInformation, Exception> MessageProcessingFailure;

        event Action<CurrentMessageInformation, Exception> MessageProcessingCompleted;

        event Action<CurrentMessageInformation> BeforeMessageTransactionRollback;

        event Action<CurrentMessageInformation> BeforeMessageTransactionCommit;

        event Action<CurrentMessageInformation, Exception> AdministrativeMessageProcessingCompleted;

        event Action Started;
    }
}
