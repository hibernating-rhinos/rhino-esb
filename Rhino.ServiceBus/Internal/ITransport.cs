using System;
using System.IO;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Internal
{
    public interface ITransport : IDisposable
    {
        void Start();

        Endpoint Endpoint { get; }
        int ThreadCount { get; }

        void Send(Endpoint endpoint, params object[] msgs);
        void Send(Endpoint endpoint, DateTime processAgainAt, object[] msgs);

        void Reply(params object[] messages);

        event Action<CurrentMessageInformation> MessageSent;

        event Func<CurrentMessageInformation,bool> AdministrativeMessageArrived;
        
        event Func<CurrentMessageInformation, bool> MessageArrived;

        event Action<CurrentMessageInformation, Exception> MessageSerializationException;
        
        event Action<CurrentMessageInformation, Exception> MessageProcessingFailure;

        event Action<CurrentMessageInformation, Exception> MessageProcessingCompleted;

        event Action<CurrentMessageInformation, Exception> AdministrativeMessageProcessingCompleted;

        event Action Started;
    }
}