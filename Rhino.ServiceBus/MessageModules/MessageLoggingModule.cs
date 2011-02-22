using System;
using System.Linq;
using System.Transactions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.MessageModules
{
    public class MessageLoggingModule : IMessageModule
    {
        private readonly Endpoint logEndpoint;
        private ITransport transport;

        [ThreadStatic]
        private static DateTime messageArrival;

        public Uri LogQueue
        {
            get { return logEndpoint.Uri; }
        }

        public MessageLoggingModule(IEndpointRouter endpointRouter, Uri logQueue)
        {
            logEndpoint = endpointRouter.GetRoutedEndpoint(logQueue);
        }

        public void Init(ITransport transport, IServiceBus bus)
        {
            this.transport = transport;
            transport.MessageArrived += Transport_OnMessageArrived;
            transport.MessageProcessingFailure += Transport_OnMessageProcessingFailure;
            transport.MessageProcessingCompleted += Transport_OnMessageProcessingCompleted;
            transport.MessageSerializationException += Transport_OnMessageSerializationException;
            transport.MessageSent += Transport_OnMessageSent;
        }

        public void Stop(ITransport transport, IServiceBus bus)
        {
            transport.MessageArrived -= Transport_OnMessageArrived;
            transport.MessageProcessingFailure -= Transport_OnMessageProcessingFailure;
            transport.MessageProcessingCompleted -= Transport_OnMessageProcessingCompleted;
            transport.MessageSerializationException -= Transport_OnMessageSerializationException;
            transport.MessageSent -= Transport_OnMessageSent;
        }

        private void Transport_OnMessageSent(CurrentMessageInformation info)
        {
            if (info.AllMessages.OfType<ILogMessage>().Any())
                return;

            Send(new MessageSentMessage
            {
                MessageId = info.MessageId,
                Source = info.Source,
                Message = info.AllMessages,
                MessageType = info.AllMessages[0].ToString(),
                Timestamp = DateTime.Now,
                Destination = info.Destination
            });
        }

        private void Send(object obj)
        {
            transport.Send(logEndpoint, new []{obj});
        }

        private void Transport_OnMessageSerializationException(CurrentMessageInformation info, Exception t)
        {
            Send(new SerializationErrorMessage
            {
                MessageId = info.MessageId,
                Error = t.ToString(),
                Source = info.Source,
            });
        }

        private void Transport_OnMessageProcessingCompleted(CurrentMessageInformation info, Exception ex)
        {
            var timestamp = DateTime.Now;
            Send(new MessageProcessingCompletedMessage
            {
                Timestamp = timestamp,
                Duration = timestamp - messageArrival,
                MessageType = info.Message.ToString(),
                MessageId = info.MessageId,
                Source = info.Source,
            });
        }

        internal void Transport_OnMessageProcessingFailure(CurrentMessageInformation info, Exception e)
        {
            string messageType = (info.Message ?? "no message").ToString();
            SendInSingleTransaction(new MessageProcessingFailedMessage
            {
                ErrorText = e.ToString(),
                Timestamp = DateTime.Now,
                MessageType = messageType,
                MessageId = info.MessageId,
                Source = info.Source,
                Message = info.Message
            });
        }

        private void SendInSingleTransaction(object msg)
        {
            using (var tx = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                Send(msg);
                tx.Complete();
            }
        }

        private bool Transport_OnMessageArrived(CurrentMessageInformation info)
        {
            messageArrival = DateTime.Now;
            Send(new MessageArrivedMessage
            {
                Timestamp = messageArrival,
                MessageType = info.Message.ToString(),
                MessageId = info.MessageId,
                Source = info.Source,
                Message = info.Message
            });
            return false;
        }
    }
}