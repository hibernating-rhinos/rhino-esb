using System;
using System.Messaging;
using log4net;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Msmq.TransportActions
{
    public class ErrorAction : ITransportAction
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(ErrorAction));
        private readonly int numberOfRetries;
        private readonly Hashtable<Guid, ErrorCounter> failureCounts = new Hashtable<Guid, ErrorCounter>();
        private readonly IQueueStrategy queueStrategy;

        public ErrorAction(int numberOfRetries, IQueueStrategy queueStrategy)
        {
            this.numberOfRetries = numberOfRetries;
            this.queueStrategy = queueStrategy;
        }

        public void Init(IMsmqTransport transport, OpenedQueue queue)
        {
            transport.MessageSerializationException += Transport_OnMessageSerializationException;
            transport.MessageProcessingFailure += Transport_OnMessageProcessingFailure;
            transport.MessageProcessingCompleted += Transport_OnMessageProcessingCompleted;
        }

        private void Transport_OnMessageSerializationException(CurrentMessageInformation information, Exception exception)
        {
            failureCounts.Write(writer => writer.Add(information.MessageId, new ErrorCounter
            {
                FailureCount = numberOfRetries + 1,
                ExceptionText = exception.ToString()
            }));
        }

        private void Transport_OnMessageProcessingCompleted(CurrentMessageInformation information, Exception ex)
        {
            if (ex != null)
                return;

            ErrorCounter val = null;
            var id = information.MessageId;
            failureCounts.Read(reader => reader.TryGetValue(id, out val));
            if (val == null)
                return;
            failureCounts.Write(writer => writer.Remove(id));
        }

        private void Transport_OnMessageProcessingFailure(CurrentMessageInformation information, Exception exception)
        {
            var id = information.MessageId;
            failureCounts.Write(writer =>
            {
                ErrorCounter errorCounter;
                if (writer.TryGetValue(id, out errorCounter) == false)
                {
                    errorCounter = new ErrorCounter
                    {
                        ExceptionText = exception.ToString(),
                        FailureCount = 0
                    };
                    writer.Add(id, errorCounter);
                }
                errorCounter.FailureCount += 1;
            });
        }

        public bool CanHandlePeekedMessage(Message message)
        {
            return true; ;
        }

        public bool HandlePeekedMessage(IMsmqTransport transport, OpenedQueue queue, Message message)
        {
            bool doesNotHaveMessageId = message.Extension.Length < 16;
            if(doesNotHaveMessageId)
            {
                var errorMessage = "Message does not have Extension set to at least 16 bytes, which will be used as the message id. Probably not a bus message.";
                transport.RaiseMessageSerializationException(queue,message,errorMessage);
                MoveToErrorQueue(queue, message, errorMessage);
                return true;
            }

            var id = message.GetMessageId();
            ErrorCounter errorCounter = null;

            failureCounts.Read(reader => reader.TryGetValue(id, out errorCounter));

            if (errorCounter == null)
                return false;

            if (errorCounter.FailureCount < numberOfRetries)
                return false;

            failureCounts.Write(writer =>
            {
                writer.Remove(id);
                MoveToErrorQueue(queue, message, errorCounter.ExceptionText);
            });

            return true;
        }

        private void MoveToErrorQueue(OpenedQueue queue, Message message, string exceptionText)
        {
            string msgId;
            if(queueStrategy.TryMoveMessage(queue, message, SubQueue.Errors,out msgId) == false)
                return;
            
            var desc = new Message
			{
				Label = ("Error description for: " + message.Label).EnsureLabelLength(),
				Body = exceptionText,
                CorrelationId = msgId
			}.SetSubQueueToSendTo(SubQueue.Errors);
			queue.Send(desc);

        	logger.WarnFormat("Moving message {0} to errors subqueue because: {1}", message.Id,
                              exceptionText);
        }

        private class ErrorCounter
        {
            public string ExceptionText;
            public int FailureCount;
        }

    }
}
