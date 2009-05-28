using System;
using System.Text;
using Rhino.Queues;
using Rhino.Queues.Model;
using Rhino.ServiceBus.DataStructures;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Transport;

namespace Rhino.ServiceBus.RhinoQueues
{
    public class ErrorAction
    {
        private readonly int numberOfRetries;
        private readonly Hashtable<string, ErrorCounter> failureCounts = new Hashtable<string, ErrorCounter>();

        public ErrorAction(int numberOfRetries)
        {
            this.numberOfRetries = numberOfRetries;
        }

        public void Init(ITransport transport)
        {
            transport.MessageSerializationException += Transport_OnMessageSerializationException;
            transport.MessageProcessingFailure += Transport_OnMessageProcessingFailure;
            transport.MessageProcessingCompleted += Transport_OnMessageProcessingCompleted;
            transport.MessageArrived += Transport_OnMessageArrived;
        }

        private bool Transport_OnMessageArrived(CurrentMessageInformation information)
        {
            var info = (RhinoQueueCurrentMessageInformation) information;
            ErrorCounter val = null;
            failureCounts.Read(reader => reader.TryGetValue(info.TransportMessageId, out val));
            if(val == null || val.FailureCount < numberOfRetries)
                return false;

            var result = false;
            failureCounts.Write(writer =>
            {
                if (writer.TryGetValue(info.TransportMessageId, out val) == false)
                    return;

                info.Queue.MoveTo(SubQueue.Errors.ToString(), info.TransportMessage);
                info.Queue.EnqueueDirectlyTo(SubQueue.Errors.ToString(), new MessagePayload
                {
                    Data = val.ExceptionText == null ? null : Encoding.Unicode.GetBytes(val.ExceptionText),
                    Headers =
                        {
                            {"correlation-id", info.TransportMessageId},
                            {"retries", val.FailureCount.ToString()}
                        }
                });

                result = true;
            });

            return result;
        }

        private void Transport_OnMessageSerializationException(CurrentMessageInformation information, Exception exception)
        {
            var info = (RhinoQueueCurrentMessageInformation) information;
            failureCounts.Write(writer => writer.Add(info.TransportMessageId, new ErrorCounter
            {
                ExceptionText = exception == null ? null : exception.ToString(),
                FailureCount = numberOfRetries + 1
            }));
        }

        private void Transport_OnMessageProcessingCompleted(CurrentMessageInformation information, Exception ex)
        {
            if (ex != null)
                return;

            ErrorCounter val = null;
            failureCounts.Read(reader => reader.TryGetValue(information.TransportMessageId, out val));
            if (val == null)
                return;
            failureCounts.Write(writer => writer.Remove(information.TransportMessageId));
        }

        private void Transport_OnMessageProcessingFailure(CurrentMessageInformation information, Exception exception)
        {
            failureCounts.Write(writer =>
            {
                ErrorCounter errorCounter;
                if (writer.TryGetValue(information.TransportMessageId, out errorCounter) == false)
                {
                    errorCounter = new ErrorCounter
                    {
                        ExceptionText = exception == null ? null : exception.ToString(),
                        FailureCount = 0
                    };
                    writer.Add(information.TransportMessageId, errorCounter);
                }
                errorCounter.FailureCount += 1;
            });
        }

        private class ErrorCounter
        {
            public string ExceptionText;
            public int FailureCount;
        }

    }
}