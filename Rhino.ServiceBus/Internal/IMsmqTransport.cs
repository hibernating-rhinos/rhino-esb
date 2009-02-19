using System;
using System.Messaging;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.Internal
{
    public interface IMsmqTransport : ITransport
    {
        void RaiseAdministrativeMessageProcessingCompleted(CurrentMessageInformation information, Exception ex);

        bool RaiseAdministrativeMessageArrived(CurrentMessageInformation information);

        void ReceiveMessageInTransaction(OpenedQueue queue, 
            string messageId, 
            Func<CurrentMessageInformation, bool> messageArrived,
            Action<CurrentMessageInformation, Exception> messageProcessingCompleted);

        void RaiseMessageSerializationException(OpenedQueue queue, Message msg, string errorMessage);
        OpenedQueue CreateQueue();
    }
}