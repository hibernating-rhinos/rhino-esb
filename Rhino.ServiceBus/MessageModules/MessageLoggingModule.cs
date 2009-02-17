using System;
using System.Messaging;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Msmq;

namespace Rhino.ServiceBus.MessageModules
{
    public class MessageLoggingModule : IMessageModule
    {
        private readonly IMessageSerializer messageSerializer;
        private readonly IEndpointRouter endpointRouter;
        private readonly Uri logQueue;
        private OpenedQueue queue;

        public MessageLoggingModule(IMessageSerializer messageSerializer, IEndpointRouter endpointRouter, Uri logQueue)
        {
            this.messageSerializer = messageSerializer;
            this.endpointRouter = endpointRouter;
            this.logQueue = logQueue;
        }

        public void Init(ITransport transport)
        {
        	var endpoint = endpointRouter.GetRoutedEndpoint(logQueue);
        	var queueInfo = MsmqUtil.GetQueuePath(endpoint);
			queueInfo.Create();
        	queue = queueInfo.Open(QueueAccessMode.Send);

            transport.MessageArrived += Transport_OnMessageArrived;
            transport.MessageProcessingFailure += Transport_OnMessageProcessingFailure;
            transport.MessageProcessingCompleted += Transport_OnMessageProcessingCompleted;
            transport.MessageSerializationException += Transport_OnMessageSerializationException;
            transport.MessageSent+=Transport_OnMessageSent;
        }

        public void Stop(ITransport transport)
        {
            transport.MessageArrived -= Transport_OnMessageArrived;
            transport.MessageProcessingFailure -= Transport_OnMessageProcessingFailure;
            transport.MessageProcessingCompleted -= Transport_OnMessageProcessingCompleted;
            transport.MessageSerializationException -= Transport_OnMessageSerializationException;
            transport.MessageSent -= Transport_OnMessageSent;

            queue.Dispose();
        }

        private void Transport_OnMessageSent(CurrentMessageInformation info)
        {
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
            var msg = new Message
            {
            	Label = obj.ToString()
            };
            messageSerializer.Serialize(new[] { obj }, msg.BodyStream);
            queue.Send(msg);
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
            Send(new MessageProcessingCompletedMessage
            {
                Timestamp = DateTime.Now,
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
    		var message = new Message
    		{
    			Label = msg.ToString()
    		};
			messageSerializer.Serialize(new[]{msg},message.BodyStream);
    		queue.SendInSingleTransaction(message);
    	}

    	private bool Transport_OnMessageArrived(CurrentMessageInformation info)
        {
            Send(new MessageArrivedMessage
            {
                Timestamp = DateTime.Now,
                MessageType = info.Message.ToString(),
                MessageId = info.MessageId,
                Source = info.Source,
                Message = info.Message
            });
            return false;
        }
    }
}