using System;
using System.Messaging;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Internal
{
    public interface IMsmqTransport : ITransport
    {
        OpenedQueue Queue { get; }

        void RaiseAdministrativeMessageProcessingCompleted(CurrentMessageInformation information, Exception ex);

        bool RaiseAdministrativeMessageArrived(CurrentMessageInformation information);

        void ReceiveMessageInTransaction(string messageId, 
            Func<CurrentMessageInformation, bool> messageArrived,
            Action<CurrentMessageInformation, Exception> messageProcessingCompleted);

        void RaiseMessageSerializationException(Message msg, string errorMessage);
    }
}