using System;
using System.Collections.Generic;
using System.Messaging;
using System.Runtime.Serialization;
using System.Transactions;
using log4net;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Msmq.TransportActions;

namespace Rhino.ServiceBus.Msmq
{
    public class MsmqTransport : AbstractMsmqListener, IMsmqTransport
	{
		[ThreadStatic]
		private static MsmqCurrentMessageInformation currentMessageInformation;

        public static MsmqCurrentMessageInformation CurrentMessageInformation
        {
            get { return currentMessageInformation; }
        }

        private readonly ILog logger = LogManager.GetLogger(typeof(MsmqTransport));
        private readonly ITransportAction[] transportActions;

        public MsmqTransport(IMessageSerializer serializer, IQueueStrategy queueStrategy, Uri endpoint, int threadCount, ITransportAction[] transportActions, IEndpointRouter endpointRouter)
            :base(queueStrategy,endpoint, threadCount, serializer,endpointRouter)
        {
            this.transportActions = transportActions;
        }

        #region ITransport Members

        protected override void BeforeStart()
        {
            foreach (var messageAction in transportActions)
            {
                messageAction.Init(this);
            }
        }

        public void Reply(params object[] messages)
		{
			if (currentMessageInformation == null)
				throw new TransactionException("There is no message to reply to, sorry.");

            Send(endpointRouter.GetRoutedEndpoint(currentMessageInformation.Source), messages);
		}

        public event Action<CurrentMessageInformation> MessageSent;
	    
        public event Func<CurrentMessageInformation, bool> AdministrativeMessageArrived;
		
        public event Func<CurrentMessageInformation, bool> MessageArrived;
		
        public event Action<CurrentMessageInformation, Exception> MessageProcessingFailure;
        
        public event Action<CurrentMessageInformation, Exception> MessageProcessingCompleted;
        
        public event Action<CurrentMessageInformation, Exception> AdministrativeMessageProcessingCompleted;

		public void Discard(object msg)
		{
			var message = GenerateMsmqMessageFromMessageBatch(new[] { msg });

            SendMessageToQueue(message.SetSubQueueToSendTo(SubQueue.Discarded), Endpoint);
		}

	    public bool RaiseAdministrativeMessageArrived(CurrentMessageInformation information)
	    {
            var copy = AdministrativeMessageArrived;
            if (copy != null)
                return copy(information);
	        return false;
        }

	    public OpenedQueue Queue
	    {
	        get { return queue; }
	    }

	    public void RaiseAdministrativeMessageProcessingCompleted(CurrentMessageInformation information, Exception ex)
	    {
	        var copy = AdministrativeMessageProcessingCompleted;
            if (copy != null)
                copy(information, ex);
	    }

	    public void Send(Endpoint endpoint, DateTime processAgainAt, object[] msgs)
		{
			if (HaveStarted == false)
				throw new InvalidOperationException("Cannot send a message before transport is started");
			
			var message = GenerateMsmqMessageFromMessageBatch(msgs);
	        var bytes = new List<byte>(message.Extension);
	        bytes.AddRange(BitConverter.GetBytes(processAgainAt.ToBinary()));
	        message.Extension = bytes.ToArray();
			message.AppSpecific = (int)MessageType.TimeoutMessageMarker;

            SendMessageToQueue(message, endpoint);
		}

        public void Send(Endpoint endpoint, params object[] msgs)
		{
			if(HaveStarted==false)
				throw new InvalidOperationException("Cannot send a message before transport is started");

			var message = GenerateMsmqMessageFromMessageBatch(msgs);

            SendMessageToQueue(message, endpoint);

			var copy = MessageSent;
			if (copy == null)
				return;

			copy(new CurrentMessageInformation
			{
				AllMessages = msgs,
				Source = Endpoint.Uri,
				Destination = endpoint.Uri,
                MessageId = message.GetMessageId(),
			});
		}

		public event Action<CurrentMessageInformation, Exception> MessageSerializationException;

		#endregion

        public void ReceiveMessageInTransaction(string messageId, Func<CurrentMessageInformation, bool> messageArrived, Action<CurrentMessageInformation, Exception> messageProcessingCompleted)
		{
			using (var tx = new TransactionScope(TransactionScopeOption.Required, GetTransactionTimeout()))
			{
				var message = queue.TryGetMessageFromQueue(messageId);
                
                if (message == null)
                    return;// someone else got our message, better luck next time

                ProcessMessage(message, queue, tx, messageArrived, messageProcessingCompleted);
			}
		}

        public void RaiseMessageSerializationException(Message msg, string errorMessage)
        {
            var copy = MessageSerializationException;
            if (copy == null)
                return;
            var messageInformation = new MsmqCurrentMessageInformation
            {
                MsmqMessage = msg,
                Queue = queue,
                Message = null,
                Source = queue.Uri,
                MessageId = Guid.Empty
            };
            copy(messageInformation, new SerializationException(errorMessage));
        }

        private void HandleMessageCompletion(
			Message message,
			TransactionScope tx,
            OpenedQueue messageQueue,
			Exception exception)
		{
			if (exception == null)
			{
				try
				{
					if (tx != null)
						tx.Complete();
					return;
				}
				catch (Exception e)
				{
					logger.Warn("Failed to complete transaction, moving to error mode", e);
				}
			}
			if (message == null)
				return;

            try
            {
                Action<CurrentMessageInformation, Exception> copy = MessageProcessingFailure;
                if (copy != null)
                    copy(currentMessageInformation, exception);
            }
            catch (Exception moduleException)
            {
                string exMsg = "";
                if (exception != null)
                    exMsg = exception.Message;
                logger.Error("Module failed to process message failure: " + exMsg,
                                             moduleException);
            }

            if (messageQueue.IsTransactional == false)// put the item back in the queue
			{
                messageQueue.Send(message);
			}
		}

        private void ProcessMessage(
			Message message, 
            OpenedQueue messageQueue, 
            TransactionScope tx,
            Func<CurrentMessageInformation, bool> messageRecieved,
            Action<CurrentMessageInformation, Exception> messageCompleted)
		{
		    Exception ex = null;
		    currentMessageInformation = CreateMessageInformation(message, null, null);
            try
            {
                //deserialization errors do not count for module events
                object[] messages = DeserializeMessages(messageQueue, message, MessageSerializationException);
                try
                {
                    foreach (object msg in messages)
                    {
                        currentMessageInformation = CreateMessageInformation(message, messages, msg);

                        if(ProcessSingleMessage(messageRecieved)==false)
                            Discard(currentMessageInformation.Message);
                    }
                }
                catch (Exception e)
                {
                    ex = e;
                    logger.Error("Failed to process message", e);
                }
                finally
                {
                    try
                    {
                        if (messageCompleted != null)
                            messageCompleted(currentMessageInformation, ex);
                    }
                    catch (Exception e)
                    {
                        logger.Error("An error occured when raising the MessageCompleted event, the error will NOT affect the message processing", e);
                    }
                }
            }
            catch (Exception e)
            {
                ex = e;
                logger.Error("Failed to deserialize message", e);
            }
            finally
		    {
                HandleMessageCompletion(message, tx, messageQueue, ex);
                currentMessageInformation = null;
		    } 
		}

	    private static bool ProcessSingleMessage(Func<CurrentMessageInformation, bool> messageRecieved)
	    {
	        if (messageRecieved == null)
	            return false;
	        foreach (Func<CurrentMessageInformation, bool> func in messageRecieved.GetInvocationList())
	        {
	            if (func(currentMessageInformation))
	            {
	                return true;
	            }
	        }
	        return false;
	    }

	    private MsmqCurrentMessageInformation CreateMessageInformation(Message message, object[] messages, object msg)
	    {
	        return new MsmqCurrentMessageInformation
	        {
                MessageId = message.GetMessageId(),
	            AllMessages = messages,
	            Message = msg,
	            Queue = queue,
                TransportMessageId = message.Id,
	            Destination = Endpoint.Uri,
	            Source = MsmqUtil.GetQueueUri(message.ResponseQueue),
	            MsmqMessage = message,
	            TransactionType = queue.GetTransactionType()
	        };
	    }

        private void SendMessageToQueue(Message message, Endpoint endpoint)
		{
			if (HaveStarted == false)
				throw new TransportException("Cannot send message before transport is started");

            try
			{
				using (var sendQueue = MsmqUtil.GetQueuePath(endpoint).Open(QueueAccessMode.Send))
				{
					sendQueue.Send(message);
					logger.DebugFormat("Send message {0} to {1}", message.Label, endpoint);
				}
			}
			catch (Exception e)
			{
				throw new TransactionException("Failed to send message to " + endpoint, e);
			}
		}

        protected override void HandlePeekedMessage(Message message)
        {
            foreach (var action in transportActions)
            {
                if(action.CanHandlePeekedMessage(message)==false)
                    continue;

                if (action.HandlePeekedMessage(this, queue, message))
                    return;
            }

            ReceiveMessageInTransaction(message.Id, MessageArrived, MessageProcessingCompleted);
        }
	}
}
